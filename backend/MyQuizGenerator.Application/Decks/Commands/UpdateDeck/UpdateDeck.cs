using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Decks.Commands.UpdateDeck;

public record UpdateDeckCommand(Guid Id, UpdateDeckRequest Request) : IRequest;

public class UpdateDeckCommandHandler : IRequestHandler<UpdateDeckCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDeckRepository _deckRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateDeckCommandHandler(
        IUnitOfWork unitOfWork,
        IDeckRepository deckRepository,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _deckRepository = deckRepository;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateDeckCommand request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepository.GetByIdAsync(request.Id, cancellationToken);

        if (deck == null)
        {
            throw new NotFoundException(nameof(Deck), request.Id);
        }

        if (deck.OwnerId != _currentUserService.UserId)
        {
            throw new ForbiddenException("You do not have permission to update this deck.");
        }

        // Update deck metadata
        deck.Name = request.Request.Name;
        deck.Description = request.Request.Description;
        deck.Status = request.Request.Status;
        deck.Visibility = request.Request.Visibility;
        deck.Tags = request.Request.Tags;
        deck.UpdatedAt = DateTime.UtcNow;
        deck.ThumbnailUrl = request.Request.ThumbnailUrl;

        _deckRepository.Update(deck);

        // Handle questions to add
        if (request.Request.QuestionsToAdd.Count > 0)
        {
            var newQuestions = request.Request.QuestionsToAdd.Select(q => new Question
            {
                Content = q.Content,
                Type = q.Type,
                Hint = q.Hint,
                Explanation = q.Explanation,
                Options = q.Options,
                CorrectAnswers = q.CorrectAnswers,
                DeckId = request.Id
            });

            await _deckRepository.AddQuestionsAsync(newQuestions, cancellationToken);
        }

        // Handle questions to update
        if (request.Request.QuestionsToUpdate.Count > 0)
        {
            var questionIds = request.Request.QuestionsToUpdate.Select(q => q.Id);
            var existingQuestions = await _deckRepository.GetQuestionsByIdsAsync(questionIds, request.Id, cancellationToken);

            foreach (var updateRequest in request.Request.QuestionsToUpdate)
            {
                var question = existingQuestions.FirstOrDefault(q => q.Id == updateRequest.Id);
                if (question != null)
                {
                    question.Content = updateRequest.Content;
                    question.Type = updateRequest.Type;
                    question.Hint = updateRequest.Hint;
                    question.Explanation = updateRequest.Explanation;
                    question.Options = updateRequest.Options;
                    question.CorrectAnswers = updateRequest.CorrectAnswers;
                }
            }
        }

        // Handle questions to delete (soft delete)
        if (request.Request.QuestionIdsToDelete.Count > 0)
        {
            var questionsToDelete = await _deckRepository.GetQuestionsByIdsAsync(
                request.Request.QuestionIdsToDelete, request.Id, cancellationToken);

            foreach (var question in questionsToDelete)
            {
                question.IsDeleted = true;
                question.DeletedAt = DateTime.UtcNow;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
