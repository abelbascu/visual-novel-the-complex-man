using System.Collections.Generic;

public class DialogueObject
{
    public int ID { get; set; }
    public List<int> DestinationDialogIDs { get; set; }
    public string DialogueTextDefault { get; set; }
    public string CatalanText {get; set;}
    public string FrenchText {get; set;}
}


