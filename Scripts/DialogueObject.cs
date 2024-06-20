using System.Collections.Generic;

public class DialogueObject
{
    public int ID { get; set; }
    public List<Dictionary<string, int>> OutgoingLinks { get; set; }
    public string DialogueTextDefault { get; set; }
    public string CatalanText {get; set;}
    public string FrenchText {get; set;}
    public string Actor {get; set;}
    public int OriginConvoID {get; set;}
    public int DestinationConvoID {get; set;}
    public int OriginDialogID {get; set;}
    public bool IsGroup = false;
    public bool IsGroupParent = false;
    public bool IsGroupChild = false;
    public bool IsNoGroupParent = false; 
    public bool IsNoGroupChild = false;
    
}


