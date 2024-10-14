using Godot;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

public partial class DialogueManager : Control {

  public const int STARTING_CONVO_ID = 1;
  public const int STARTING_DIALOGUE_ID = 1;
  //-------------------------------------------------------------------config variables---------------------------------------------------------------------------------
  [Export] public int currentConversationID = STARTING_CONVO_ID; //set here the conversation you want to load. Conversations in Chatmapper are what we could call chapters.
  [Export] public int currentDialogueID = STARTING_DIALOGUE_ID; //set here the starting dialogue of the conversation
                                                                //-----------------------------------------------------------------dependency variables------------------------------------------------------------------------------
  public Dictionary<int, List<DialogueObject>> conversationDialogues; //the int refers to the conversation ID, see 'currentConversationID' above.
  public DialogueObject currentDialogueObject { get; set; }
  public List<DialogueObject> playerChoicesList;
  //----------------------------------------------------------------------bools----------------------------------------------------------------------------------------
  public bool isDialogueBeingPrinted = false; //we don't want to print a new dialogue is we are currently displaying another one
  public bool IsPlayerChoiceBeingPrinted { get; private set; }
  //------------------------------------------------------------------event handlers-----------------------------------------------------------------------------------
  //---------------------------------------------------------------------singleton--------------------------------------------------------------------------------------
  public static DialogueManager Instance { get; private set; }
  public DialogueBoxWithTag_YD_BH dialogueBoxUI;
  public PlayerChoicesBoxU_YD_BottomHorizontal playerChoicesBoxUI;

  //public const string NARRATOR = "3";

  public Action<int> DialogueVisited;
  public event Action<DialogueObject> DialogueStarted;

  public enum Actors {
    Player = 1,
    Narrator = 3
  };


  public void SetIsPlayerChoiceBeingPrinted(bool isPrinting) {
    IsPlayerChoiceBeingPrinted = isPrinting;
  }

  public override void _EnterTree() {
    base._EnterTree();
    if (Instance == null) {
      Instance = this;
    } else {
      QueueFree();
    }
  }

  public override void _ExitTree() {
    base._ExitTree();
  }

  public override void _Ready() {

    //We have a dependency with the UIManager. 
    //We try to wait for the UIManager to enter the tree.
    CallDeferred(nameof(InitializeUI));

    // Ignore mouse input if it doesn't need to interact directly
    MouseFilter = MouseFilterEnum.Ignore;
    // Make GameManager fill its parent
    AnchorRight = 1;
    AnchorBottom = 1;
    LoadDialogueObjects("C:/PROJECTS/GODOT/visual-novel-the-complex-man/DialogueDB/dialogueDB.json");
    playerChoicesList = new();
  }

  private void InitializeUI() {
    if (UIManager.Instance == null) {
      // If UIManager is not ready, try again in the next frame
      CallDeferred(nameof(InitializeUI));
      return;
    }
    dialogueBoxUI = UIManager.Instance.GetDialogueBoxUI();
    playerChoicesBoxUI = UIManager.Instance.GetPlayerChoicesBoxUI();
    dialogueBoxUI.MouseFilter = MouseFilterEnum.Stop;
    playerChoicesBoxUI.MouseFilter = MouseFilterEnum.Stop;
  }

  private void LoadDialogueObjects(string filePath) {
    try {
      string jsonText = File.ReadAllText(filePath);
      // Deserialize JSON data and extract the required fields
      conversationDialogues = JSON2DialogueObjectParser.ExtractDialogueObjects(jsonText);
    } catch (IOException e) {
      GD.PrintErr("Error loading dialogue data: " + e.Message);
    } catch (JsonException e) {
      GD.PrintErr("Error parsing JSON data: " + e.Message);
    }
  }

  public DialogueObject GetDialogueObject(int currentConversationID, int currentDialogueObjectID) {
    // Check if the conversationID exists in the dictionary
    if (conversationDialogues.TryGetValue(currentConversationID, out List<DialogueObject> dialogueListView)) {
      // find the first DialogueObject with the specified ID, IDs are unique
      return dialogueListView.FirstOrDefault(dialogueObject => dialogueObject.ID == currentDialogueObjectID);
    }
    return null;
  }

  public async Task DisplayDialogueOrPlayerChoice(DialogueObject dialogObj) {
    // Narrator or NPC won't ever have multiple choices, so we can display the dialogue now.     

    await GameManager.Instance.HandleMediaPreDelays(dialogObj);

    DialogueStarted?.Invoke(dialogObj);

    if (dialogObj.Actor != "1") {
      DisplayDialogue(dialogObj);
      currentDialogueObject = dialogObj;
      GameManager.Instance.SchedulePostDelayMediaActions(dialogObj);
    } else {
      AddPlayerChoicesToList(dialogObj.ID, dialogObj);
      DisplayPlayerChoices(playerChoicesList, SetIsPlayerChoiceBeingPrinted);
    }
  }

  public void DisplayDialogue(DialogueObject currentDialogueObject) {
    if (isDialogueBeingPrinted) //is we are currently printing a dialogue in the DialogueBoxUI, do nothing
      return;
    isDialogueBeingPrinted = true;
    if (dialogueBoxUI == null) {
      DisplayDialogueBoxUI();
    }
    if (dialogueBoxUI != null)
      dialogueBoxUI.Show();
    if (playerChoicesBoxUI != null)
      playerChoicesBoxUI.Hide();

    dialogueBoxUI.DisplayDialogueLine(currentDialogueObject, TranslationServer.GetLocale());
    Actors actor;
    if (Enum.TryParse(currentDialogueObject.Actor, out actor)) {
      string speakerName = GetSpeakerName(actor);
      dialogueBoxUI.DisplaySpeakerName(speakerName);
    }
  }


  public string GetSpeakerName(Actors actor) {
    return actor switch {
      Actors.Player => "Player",
      Actors.Narrator => "Narrator",
      _ => "Unknown Speaker"
    };
  }

  public void DisplayPlayerChoices(List<DialogueObject> playerChoices, Action<bool> setIsPlayerChoiceBeingPrinted) {

    if (playerChoicesBoxUI == null) {
      //before adding the player choices, we need to create the container VBox
      DisplayPlayerChoicesBoxUI();
    }
    if (playerChoicesBoxUI != null) {
      //ensure the container is visible
      playerChoicesBoxUI.Show();
      //let's hide the dialogue box, that's used to displaye narrator/NPC texts, not the player's
      if (dialogueBoxUI != null)
        dialogueBoxUI.Hide();

      setIsPlayerChoiceBeingPrinted(true);
      playerChoicesBoxUI.DisplayPlayerChoices(playerChoices, TranslationServer.GetLocale());
      setIsPlayerChoiceBeingPrinted(false);
    }
  }

  public void DisplayDialogueBoxUI() {
    // Ensure the dialogue box is visible
    dialogueBoxUI.Visible = true;
    dialogueBoxUI.TopLevel = true;
    //once all chars of the dialogue text are displayed in the container, we can show the next dialogue.
    dialogueBoxUI.FinishedDisplayingDialogueLine += DialogueManager.Instance.OnTextBoxFinishedDisplayingDialogueLine;
  }

  //!why should we need to create the playerChoicesBoxUI here when we do it in the UIManager?
  public void DisplayPlayerChoicesBoxUI() {
    PackedScene scene = ResourceLoader.Load<PackedScene>("res://Scenes/PlayerChoicesBoxU_YD_BottomHorizontal.tscn");
    Node instance = scene.Instantiate();
    AddChild(instance);
    //VBoxContainer playerChoìces = instance as VBoxContainer;
    playerChoicesBoxUI = instance as PlayerChoicesBoxU_YD_BottomHorizontal;
    playerChoicesBoxUI.Show();
    playerChoicesBoxUI.TopLevel = true;
    //once all chars of the dialogue text are displayed in the container, we can show the next line.
    playerChoicesBoxUI.FinishedDisplayingPlayerChoice += DialogueManager.Instance.OnTextBoxFinishedDisplayingPlayerChoices;
  }

  //IEnumerable<int> so we can pass a list or a single int when there is only one player choice to add to the playerChoicesList
  public void AddPlayerChoicesToList(IEnumerable<int> destinationDialogIDs, DialogueObject dialogObj) {
    List<DialogueObject> newChoices = new List<DialogueObject>();
    foreach (int id in destinationDialogIDs) {
      newChoices.Add(GetDialogueObject(currentConversationID, id));
    }
    // Insert the new choices at the beginning of the list, maintaining their original order
    playerChoicesList.InsertRange(0, newChoices);
  }

  //overload method when we only have one single player choice to add to the PlayerChoicesList
  public void AddPlayerChoicesToList(int dialogID, DialogueObject dialogObj) {
    AddPlayerChoicesToList(new[] { dialogID }, dialogObj);
  }

  public void AddPlayerChoicesToList(DialogueObject dialogObject) {
    playerChoicesList.Insert(0, dialogObject);
  }

  public void OnTextBoxFinishedDisplayingPlayerChoices() {
    IsPlayerChoiceBeingPrinted = false;
    //we are already in dialogue mode, but we trigger this empty signal
    //so focusable UI controls list in InputManager is updated
    if (GameStateManager.Instance.IsInState(GameStateMachine.State.InDialogueMode, GameStateMachine.SubState.None))
      GameStateManager.Instance.Fire(GameStateMachine.Trigger.ENTER_DIALOGUE_MODE);
  }

  public void OnTextBoxFinishedDisplayingDialogueLine() {
    isDialogueBeingPrinted = false;
    //we are already in dialogue mode, but we trigger this empty signal
    //so focusable UI controls list in InputManager is updated
    if (GameStateManager.Instance.IsInState(GameStateMachine.State.InDialogueMode, GameStateMachine.SubState.None))
      GameStateManager.Instance.Fire(GameStateMachine.Trigger.ENTER_DIALOGUE_MODE);
  }

  public void RemoveFromPlayerChoicesList(DialogueObject dialogObj) {
    playerChoicesList.Remove(dialogObj);
  }

  public void OnDialogueBoxUIPressed() {

    DialogueVisited.Invoke(currentDialogueObject.ID);

    DialogueObject nextDialogObject = new();
    List<int> destinationDialogIDs = new();

    if (!isDialogueBeingPrinted) {
      UIManager.Instance.dialogueBoxUI.dialogueLineLabel.Text = "";
    } else {
      DisplayDialogueSuddenly();
      return;
    }

    //if we reached a dead end path, show again the playerChoices so the player can choose another path
    //dead end paths can be used to provide contexts, make jokes, give hints, etc.
    if (currentDialogueObject.OutgoingLinks.Count == 0) {
      DisplayPlayerChoices(playerChoicesList, SetIsPlayerChoiceBeingPrinted);
    }

    // Iterate over the OutgoingLinks list and add unique "DestinationDialogID" values to the set
    foreach (Dictionary<string, int> dict in currentDialogueObject.OutgoingLinks) {
      if (dict.ContainsKey("DestinationDialogID")) {
        destinationDialogIDs.Add(dict["DestinationDialogID"]);
      }
    }
    //here we try to get the nextDialogueObject(s) to display, but we still don't know if it's a single Narrator, new conversation or a death end. 
    // or single/multiple Player choice, and if it is a  multiple player choice we still don't know if they are part of a Group/NoGroup, 
    if (destinationDialogIDs.Count == 1) {
      var linkDict = currentDialogueObject.OutgoingLinks.FirstOrDefault(dict =>
          dict.ContainsKey("DestinationDialogID") && dict.ContainsKey("DestinationConvoID"));

      if (linkDict != null) {
        int destinationDialogID = linkDict["DestinationDialogID"];
        int destinationConversationID = linkDict["DestinationConvoID"];
        if (destinationDialogID == 0) //if we land on a new Conversation, the ID 0 is a header, so let's move to the first dialogue
          destinationDialogID = 1;
        nextDialogObject = GetDialogueObject(destinationConversationID, destinationDialogID);
        //first, if the dialogue that we clicked take us to another new conversation, let's reset the player choices list and buttons on the VBox
        if (currentConversationID != destinationConversationID) {
          currentConversationID = destinationConversationID;
          //if we are landing on a new conversation, it means we reach the end of the previosu conversatrions
          //and we can clear the unvisited player choices
          playerChoicesList.Clear();
          if (UIManager.Instance.playerChoicesBoxUI != null)
            UIManager.Instance.playerChoicesBoxUI.RemoveAllPlayerChoiceButtons();
        }
      }

      //if it's a Group node, it means it has multiple player choices. Traversing a Group subpath won't delete the other unselected Group choices and
      //the player will still be able explore them (unless the end of a subpath he is traversing takes him to a new conversation or a sure death)
      if (nextDialogObject.IsGroup == true) {
        nextDialogObject.IsGroupParent = true;
        AddGroupPlayerChoicesToList(nextDialogObject);
        DisplayPlayerChoices(playerChoicesList, SetIsPlayerChoiceBeingPrinted);
      } else {

        DisplayDialogueOrPlayerChoice(nextDialogObject);
        currentDialogueObject = nextDialogObject;
      }
    }

    //if the node is a NoGroupParent, meaning that it is not a GROUP node but it has branching childs, 
    //tag it as NoGroupParent and do the same for the children as NoGroupChild
    //NoGroupChild are exclusive, meaning that at the exact moment that a  NoGroup child player choice
    // is clicked by the user, any other child at the same level must be removed from the PlayerChoicesList
    //and those subpaths cannot be traversed anymore unless the player starts a new game. 
    // the dialogObj.Actor != 1 is to ensure that the player answers are triggered by the narrator
    else if (destinationDialogIDs.Count > 1 && currentDialogueObject.IsGroup == false) {
      //nextDialogObject.IsNoGroupParent = true;
      AddNoGroupPlayerChoicesToList(currentDialogueObject);
      DisplayPlayerChoices(playerChoicesList, SetIsPlayerChoiceBeingPrinted);
    }

    if (GameStateManager.Instance.IsInState(GameStateMachine.State.InDialogueMode, GameStateMachine.SubState.None))
      GameStateManager.Instance.Fire(GameStateMachine.Trigger.ENTER_DIALOGUE_MODE);

  }

  public void AddGroupPlayerChoicesToList(DialogueObject nextDialogueObject) {
    nextDialogueObject.IsGroupParent = true;
    List<int> destinationDialogIDs = new List<int>();

    // Iterate over the OutgoingLinks list and add unique "DestinationDialogID" values to the set
    foreach (Dictionary<string, int> dict in nextDialogueObject.OutgoingLinks) {

      if (dict.TryGetValue("DestinationDialogID", out int destinationDialogID) &&
          dict.TryGetValue("DestinationConvoID", out int destinationConvoID)) {
        DialogueObject dialogObject = GetDialogueObject(destinationConvoID, destinationDialogID);
        dialogObject.IsGroupChild = true;
        destinationDialogIDs.Add(destinationDialogID);
      }
    }

    if (destinationDialogIDs.Count > 0) {
      AddPlayerChoicesToList(destinationDialogIDs, nextDialogueObject);
    }
  }

  public void AddNoGroupPlayerChoicesToList(DialogueObject currentDialogueObject) {
    currentDialogueObject.IsNoGroupParent = true;
    List<int> destinationDialogIDs = new List<int>();
    // Iterate over the OutgoingLinks list and add unique "DestinationDialogID" values to the set
    foreach (Dictionary<string, int> dict in currentDialogueObject.OutgoingLinks) {
      if (dict.TryGetValue("DestinationDialogID", out int destinationDialogID) &&
          dict.TryGetValue("DestinationConvoID", out int destinationConvoID)) {
        DialogueObject dialogObject = GetDialogueObject(destinationConvoID, destinationDialogID);
        dialogObject.IsNoGroupChild = true;
        //we'll need the parent ID to remove any NoGroup subpaths 
        //if the button with this dialogObject is pressed by the player
        dialogObject.NoGroupParentID = dict["OriginDialogID"];
        destinationDialogIDs.Add(destinationDialogID);
      }
    }

    if (destinationDialogIDs.Count > 0) {
      AddPlayerChoicesToList(destinationDialogIDs, currentDialogueObject);
    }
  }

  public void OnPlayerChoiceButtonUIPressed(DialogueObject playerChoiceObject) {



    DialogueVisited.Invoke(playerChoiceObject.ID);

    //we need to remove first the dialogObject on playerChoicesList with the same ID as playerChoiceObject.ID
    playerChoicesList.RemoveAll(dialogObj => dialogObj.ID == playerChoiceObject.ID);
    currentDialogueObject = playerChoiceObject;
    DialogueObject nextDialogObject = new();
    List<int> destinationDialogIDs = new();

    if (IsPlayerChoiceBeingPrinted)
      return;
    // Iterate over the OutgoingLinks list and add unique "DestinationDialogID" values to the set
    foreach (Dictionary<string, int> dict in playerChoiceObject.OutgoingLinks) {
      if (dict.ContainsKey("DestinationDialogID")) {
        destinationDialogIDs.Add(dict["DestinationDialogID"]);
      }
    }

    if (playerChoiceObject.IsNoGroupChild == true) {
      RemoveAllNoGroupChildrenFromSameNoGroupParent(playerChoiceObject);
    }
    //if we reach a node where the user can't go back, let's remove all unselected player choices to force him to go that path   
    if (playerChoiceObject.IsNoTurningBackPath == true) {
      playerChoicesList.Clear();
      if (UIManager.Instance.playerChoicesBoxUI != null)
        UIManager.Instance.playerChoicesBoxUI.RemoveAllPlayerChoiceButtons();
    }
    //here we get the nextDialogueObject to display, but we still don't know if it's a Narrator, single Player choice, Group Node or No Group node
    if (destinationDialogIDs.Count == 1) {
      var linkDict = playerChoiceObject.OutgoingLinks.FirstOrDefault(dict =>
          dict.ContainsKey("DestinationDialogID") && dict.ContainsKey("DestinationConvoID"));

      if (linkDict != null) {
        int destinationDialogID = linkDict["DestinationDialogID"];
        int destinationConvoID = linkDict["DestinationConvoID"];
        nextDialogObject = GetDialogueObject(destinationConvoID, destinationDialogID);
        if (currentConversationID != destinationConvoID) {
          currentConversationID = destinationConvoID;
          playerChoicesList.Clear();
          UIManager.Instance.playerChoicesBoxUI.RemoveAllPlayerChoiceButtons();
        }
        if (nextDialogObject.IsGroup == false) {
          DisplayDialogueOrPlayerChoice(nextDialogObject);
          currentDialogueObject = nextDialogObject;
        } else if (nextDialogObject.IsGroup == true) {
          AddGroupPlayerChoicesToList(nextDialogObject);
          DisplayPlayerChoices(playerChoicesList, SetIsPlayerChoiceBeingPrinted);
        }
      }

      if (GameStateManager.Instance.IsInState(GameStateMachine.State.InDialogueMode, GameStateMachine.SubState.None))
        GameStateManager.Instance.Fire(GameStateMachine.Trigger.ENTER_DIALOGUE_MODE);
    }

    //if the node is a NoGroupParent, meaning that it is not a GROUP node but it has branching childs, 
    //tag it as NoGroupParent and do the same for the children as NoGroupChild
    //NoGroupChild are exclusive, meaning that at the exact moment that a  NoGroup child player choice
    // is clicked by the user, any other child at the same level must be removed from the PlayerChoicesList
    //and those subpaths cannot be traversed anymore unless the player starts a new game. 
    // the dialogObj.Actor != 1 is to ensure that the player answers are triggered by the narrator
    else if (destinationDialogIDs.Count > 1 && currentDialogueObject.IsGroup == false) {
      nextDialogObject.IsNoGroupParent = true;
      AddNoGroupPlayerChoicesToList(currentDialogueObject);
      DisplayPlayerChoices(playerChoicesList, SetIsPlayerChoiceBeingPrinted);
    }

    //at the moment we'll count player choices as dialogue objevts to count the Total number of dialogues visited;
    // DialogueVisited.Invoke(playerChoiceObject.ID);
  }

  public void RemoveAllNoGroupChildrenFromSameNoGroupParent(DialogueObject playerChoiceObject) {
    RemoveAllNoGroupChildrenFromPlayerChoicesList(playerChoiceObject);
    RemoveAllNoGroupChildrenFromPlayerChoicesBoxUI(playerChoiceObject);
  }

  public void RemoveAllNoGroupChildrenFromPlayerChoicesList(DialogueObject playerChoiceObject) {
    List<DialogueObject> objectsToRemove = new List<DialogueObject>();
    if (playerChoiceObject.NoGroupParentID.HasValue) {
      foreach (DialogueObject dialogObj in playerChoicesList) {
        if (dialogObj.NoGroupParentID == playerChoiceObject.NoGroupParentID)
          objectsToRemove.Add(dialogObj);
      }
    }
    foreach (var obj in objectsToRemove)
      playerChoicesList.Remove(obj);
  }

  public void RemoveAllNoGroupChildrenFromPlayerChoicesBoxUI(DialogueObject playerChoiceObject) {
    UIManager.Instance.playerChoicesBoxUI.RemoveAllNoGroupChildrenWithSameOriginID(playerChoiceObject);
  }

  public void OnPlayerChoicePressed() {
    //always that the user clicks on a player choice, we know it is always Actor = "1" and that must be stored in the PlayerCHoicesList
    //so as it has already been displayed and now we move on to the next dialogue or choices to show, let's remove it first so it's not displayed again
    //when player choices are displayed again 
    if (currentDialogueObject.Actor == "1") {
      RemoveFromPlayerChoicesList(currentDialogueObject);
    }
  }

  public void DisplayDialogueSuddenly() {
    isDialogueBeingPrinted = false;
    UIManager.Instance.dialogueBoxUI.StopLetterByLetterDisplay();
  }
}




