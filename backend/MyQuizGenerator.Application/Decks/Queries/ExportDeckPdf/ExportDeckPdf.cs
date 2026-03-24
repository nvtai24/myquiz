using MediatR;
using Microsoft.EntityFrameworkCore;
using MyQuizGenerator.Application.Common.Exceptions;
using MyQuizGenerator.Application.Common.Interfaces;
using MyQuizGenerator.Application.Common.Interfaces.Repositories;
using MyQuizGenerator.Application.Decks.DTOs;
using MyQuizGenerator.Domain.Entities;

namespace MyQuizGenerator.Application.Decks.Queries.ExportDeckPdf;

public record ExportDeckPdfQuery(Guid DeckId) : IRequest<ExportDeckPdfResponse>;

public class ExportDeckPdfQueryHandler : IRequestHandler<ExportDeckPdfQuery, ExportDeckPdfResponse>
{
    private readonly IDeckRepository _deckRepository;
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IRepository<Guid, UserSubscriptionPlan> _userSubscriptionRepository;
    private readonly IPdfService _pdfService;

    public ExportDeckPdfQueryHandler(
        IDeckRepository deckRepository,
        IUserService userService,
        ICurrentUserService currentUserService,
        IRepository<Guid, UserSubscriptionPlan> userSubscriptionRepository,
        IPdfService pdfService)
    {
        _deckRepository = deckRepository;
        _userService = userService;
        _currentUserService = currentUserService;
        _userSubscriptionRepository = userSubscriptionRepository;
        _pdfService = pdfService;
    }

    public async Task<ExportDeckPdfResponse> Handle(ExportDeckPdfQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedException("Invalid token");
        }

        await EnsureCanExportPdfAsync(userId, cancellationToken);

        var deck = await _deckRepository.GetDeckByIdWithQuestionsAsync(request.DeckId, cancellationToken)
            ?? throw new NotFoundException(nameof(Deck), request.DeckId);

        var ownerInfo = await _userService.GetUserInfoAsync(deck.OwnerId);

        var deckDetail = new DeckDetailResponse
        {
            Id = deck.Id,
            Name = deck.Name,
            Description = deck.Description,
            QuestionCount = deck.Questions.Count,
            OwnerName = ownerInfo?.FullName ?? string.Empty,
            OwnerEmail = ownerInfo?.Email ?? string.Empty,
            Questions = deck.Questions.Select(q => new QuestionResponse
            {
                Id = q.Id,
                Content = q.Content,
                Type = q.Type,
                Hint = q.Hint,
                Explanation = q.Explanation,
                Options = q.Options,
                CorrectAnswers = q.CorrectAnswers
            }).ToList()
        };

        var pdfContent = _pdfService.GenerateDeckPdf(deckDetail);

        return new ExportDeckPdfResponse
        {
            Content = pdfContent,
            FileName = $"MyQuiz_{_pdfService.BuildSafeFileName(deck.Name)}.pdf"
        };
    }

    private async Task EnsureCanExportPdfAsync(string userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var canExport = await _userSubscriptionRepository.GetQueryable()
            .Include(usp => usp.SubscriptionPlan)
            .Where(usp => usp.UserId == userId && usp.StartDate <= now && usp.EndDate > now)
            .OrderByDescending(usp => usp.SubscriptionPlan.Order)
            .Select(usp => usp.SubscriptionPlan.HasExportToPdf)
            .FirstOrDefaultAsync(cancellationToken);

        if (!canExport)
        {
            throw new ForbiddenException("Your current subscription does not allow PDF export.");
        }
    }
}
