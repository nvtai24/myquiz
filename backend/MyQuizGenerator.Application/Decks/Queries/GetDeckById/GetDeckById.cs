using MediatR;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Decks.Queries.GetDeckById;

public record GetDeckByIdQuery(Guid Id) : IRequest<DeckDetailResponse>;

public class GetDeckByIdQueryHandler : IRequestHandler<GetDeckByIdQuery, DeckDetailResponse>
{
    private readonly IDeckRepository _deckRepository;

    public GetDeckByIdQueryHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<DeckDetailResponse> Handle(GetDeckByIdQuery request, CancellationToken cancellationToken)
    {
        var deck = await _deckRepository.GetDeckByIdWithQuestionsAsync(request.Id, cancellationToken);

        if (deck == null)
        {
            throw new NotFoundException(nameof(Deck), request.Id);
        }

        // Note: Access control logic (e.g. user != owner && visibility == Private) should go here if needed.
        // For now, valid deck ID returns keys.

        return new DeckDetailResponse
        {
            Id = deck.Id,
            Name = deck.Name,
            Description = deck.Description,
            Visibility = deck.Visibility,
            Tags = deck.Tags,
            QuestionCount = deck.Questions.Count,
            ThumbnailUrl = deck.ThumbnailUrl,
            CreatedAt = deck.CreatedAt,
            UpdatedAt = deck.UpdatedAt,
            Questions = deck.Questions.Select(q => new QuestionResponse
            {
                Id = q.Id,
                Content = q.Content,
                Type = q.Type,
                Hint = q.Hint,
                Explanation = q.Explanation,
                Options = q.Options,
                CorrectAnswers = q.CorrectAnswers
            }).ToList(),
            Documents = deck.Documents.Select(d => new DeckDocumentResponse
            {
                FileName = d.OriginalFileName,
                Url = d.Url,
                ContentType = d.ContentType,
                Size = d.Size
            }).ToList()
        };
    }
}
