using System.ComponentModel.DataAnnotations;

namespace MyQuizGenerator.Application.DeckInvitations.DTOs;

public class CreateDeckInvitationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
