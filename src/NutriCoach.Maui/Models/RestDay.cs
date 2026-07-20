namespace NutriCoach.App.Models;

/// <summary>Ein bewusst markierter Ruhetag - zählt nicht als Trainingstag, aber auch nicht als "verpasst".</summary>
public class RestDay
{
    public int Id { get; set; }
    public int UserProfileId { get; set; }
    public DateOnly Date { get; set; }
}
