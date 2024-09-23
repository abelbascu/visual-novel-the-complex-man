using System.Collections.Generic;

public class DialogueObject {
  public int ID { get; set; }
  public List<Dictionary<string, int>> OutgoingLinks { get; set; }
  public string DialogueTextDefault { get; set; }
  public string CatalanText { get; set; }
  public string FrenchText { get; set; }
  public string Actor { get; set; }
  //public int OriginConvoID {get; set;}
  //public int DestinationConvoID {get; set;}
  //public int OriginDialogID {get; set;}
  public bool IsGroup = false;
  public bool IsGroupParent = false;
  public bool IsGroupChild = false;
  public bool IsNoGroupParent = false;
  public bool IsNoGroupChild = false;
  public int? NoGroupParentID = null;
  //if the player selects this choice, all the unvisited playerChoices will be deleted.
  //and the player can only follow the current path, until a sure dead, change of conversation, etc.
  public bool IsNoTurningBackPath = false;
  public string VisualPath { get; set; }
  public float VisualPreDelay { get; set; }
  public float VisualPostDelay { get; set; }
  public string MusicPath { get; set; }
  public float MusicPreDelay { get; set; }
  public float MusicPostDelay { get; set; }
  public string SoundPath { get; set; }
  public float SoundPreDelay { get; set; }
  public float SoundPostDelay { get; set; }
}


