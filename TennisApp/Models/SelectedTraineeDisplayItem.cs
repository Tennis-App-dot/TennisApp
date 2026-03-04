namespace TennisApp.Models;

/// <summary>
/// Display item for selected trainee list with row index.
/// </summary>
public class SelectedTraineeDisplayItem
{
    public int Index { get; set; }
    public string TraineeId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;

    public static SelectedTraineeDisplayItem FromTrainee(TraineeItem trainee, int index)
    {
        return new SelectedTraineeDisplayItem
        {
            Index = index,
            TraineeId = trainee.TraineeId,
            FullName = trainee.FullName,
            Nickname = trainee.Nickname ?? string.Empty
        };
    }
}
