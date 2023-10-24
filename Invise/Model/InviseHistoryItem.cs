namespace Invise.Model;
public class InviseHistoryItem
{
    public string Time { get; set; }
    public string Description { get; set; }
    public string Link { get; set; }

    public InviseHistoryItem(string time, string description, string link)
    {
        Time = time;
        Description = description;
        Link = link;
    }
}
