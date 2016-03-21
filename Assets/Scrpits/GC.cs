using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;

public class GC : MonoBehaviour 
{

	//static random generator
	static System.Random rnd = new System.Random();

	//game mechanics
	static float MAXHITPOINTS = 100;						//your maximum hitpoints.
	static float DAMAGE = 20;								//how many hitpoints you lose when you present bad evidence.
	float hitPoints = MAXHITPOINTS;							//sets your starting hit points.
	float healthBarLength;									//the length of the health bar
	Boolean gameOver = false;								//checks if gameOver.

	//evidence information
	IDictionary<string,ItemScript> inventoryList = new Dictionary<string,ItemScript>();	//the dictionary that holds the items for the dialogue Tree/LinkedList list
	public bool presentEvidence = false;			//is evidence currently being presented. Used to unhide and select the evidence.
	public ItemScript selectedItem = null;			//what item is selected for presenting and viewing description.
	public ItemScript item;							//prefab to be generated.
	string itemString = "";							// a string that displays when an item is added to your inventory.
	public string thisInventory = "";				//the location of the current file being read.

	//dialogue variables
	public string str;								//the text that's visible on the screen.  This gets updated one at a time to allow for scrolling text
	string strComplete;								//the completed text
	int diagCounter = 0;							//what character are we currently on.
	const float DIAGDELAY = .25f;					//how long a delay between each character appearing
	float diagTimer = DIAGDELAY;					//the timer counting down until the next character, the smaller the faster
	const int DIAGFAST = 30;						//how much faster the timer ticks down if the speed up button is pushed
	Boolean diagPause = false;						//a boolean to check that the speed up or skip forward button has been released to avoid skipping though multiple screens.
	public Boolean loop = false;					//a check to see if the dialogue should loop. Typically cross-examines loop, regular dialogues do not.

	//dialogue information - the dialogue is kept in a series of Dialogue items, help in a dictionary. However, they are arranged in a linked list to create an easy to access dialogue tree.
	public Dialogue start = null;					// the starting item for the dialogue.
	public Dialogue current = null;					// keeps track of which dialogue is currently being dispalayed
	public Dialogue returnDialogue = null;			// a temprary value used to keep track of where the dialogue was when left (at this point, only used for presenting failed evidence)
	List<Dialogue> startFail = new List<Dialogue>();///an array holding the beginnings of failure dialogues to be pulle dout reandomly when necessary.
	public Dialogue GameOver = null;				// the start of the game over dialgoue tree
	IDictionary<string,Dialogue> dialogueTree = new Dictionary<string,Dialogue>();	//the dictionary that holds the items for the dialogue Tree/LinkedList list
	public string thisTree = "";					//the location of the current file being read.
	public string nextTree = "";					//the location of the next file to read.

	//reading in text information
	public TextAsset textFile;     			// drop your file here in inspector
	readonly char []LINE_DELIM	= {'\n'};	// Deliminates lines in startup file
	readonly char []LABEL_DELIM = {'|'};	// deliminates between the label and body of the text

	//controls for moving through dialogue trees
	KeyCode keyNext = KeyCode.RightArrow;	//move forward in dialogue trees, default to right arrow
	KeyCode keyPrev = KeyCode.LeftArrow;	//move backward in dialogue trees, default to left arrow
	KeyCode keyPress = KeyCode.UpArrow;		//move to the press option in dialogue trees, defaults to up arrow
	KeyCode keyEvidence = KeyCode.DownArrow;//activate presenting evidence options.
	KeyCode keyBack = KeyCode.UpArrow;		//activate to leave evidence options.
	KeyCode keyPresent = KeyCode.DownArrow;	//presents the selected evidence.
	KeyCode keyFast = KeyCode.Space;		//speed up current dialogue text, default to space bar

	// Use this for initialization
	void Start () 
	{
		LoadInventory ("/Resources/Dialogues/start_evidence.txt");	//loads the test inventory, this will eventually be removed.
		LoadDiag ("/Resources/Dialogues/start.txt");		//loads the test dialogue, this will eventually be removed.
	}

	void SavingGame()
	{
		string saveStates = "";								//creates a string to store the state of all the dialogues

		//goes through each Dialogue in the DialogueTree and stores their state into a giant string
		foreach (KeyValuePair<string, Dialogue> diag in dialogueTree) 
		{
			saveStates += diag.Value.getState ();
		}

		//goes through each ItemScript in the InventoryList and stores their owned state into a giant string
		foreach (KeyValuePair<string, ItemScript> item in inventoryList) 
		{
			if (item.Value.isOwned ())
				saveStates += "1";					//state 1 if it is owned.
			else
				saveStates += "0";					//state 0 if it is not
		}

		// Saving a saved game.
		MySaveGame mySaveGame1 = new MySaveGame();			//generates the save data
		mySaveGame1.thisTree = thisTree;					// stores the location of the current dialogue tree text file
		mySaveGame1.thisInventory = thisInventory;			// stores the location of the current dialogue tree text file
		mySaveGame1.hitPoints = hitPoints;					// stores your current hit points
		mySaveGame1.saveStates = saveStates;				//stores the string of states
		mySaveGame1.current = current.getKey ();			//stores the key for the current Dialogue

		SaveGameSystem.SaveGame(mySaveGame1, "MySaveGame"); // Saves as MySaveGame.sav

		// Deleting a saved game.
//		SaveGameSystem.DeleteSaveGame("MySaveGame");

	}

	void LoadingGame()
	{
		// Loading a saved game.
		MySaveGame mySaveGame2 = SaveGameSystem.LoadGame ("MySaveGame") as MySaveGame;


//		itemList = new ArrayList();   	 			//wipes out the inventory
		LoadInventory (mySaveGame2.thisInventory);		//loads the dialogue tree that the save game was on.
		LoadDiag (mySaveGame2.thisTree);			//loads the dialogue tree that the save game was on.
		hitPoints = mySaveGame2.hitPoints;			//loads the hit points
		gameOver = false;							//makes sure gameOver isn't true
		// goes through the dialogueTree.  Any Dialogue who's state has been changed, needs to be updated.
		int i = 0;									//a counter to follow the string.
		foreach (KeyValuePair<string, Dialogue> diag in dialogueTree) 
		{
			if (mySaveGame2.saveStates[i] == '1')									//marks that this Dialogue has been changed
			{
				if (diag.Value.getNewNext() != null)								//checks for a new Next Dialogue
				{
					diag.Value.getNext().setPrev(diag.Value.getNewNext());			//sets the next Dialogue's prev to link up to the added Dialogue
					diag.Value.setNext(diag.Value.getNewNext());					//sets the current Dialogue's next to link to the added dialogue.
					diag.Value.setState(1);											//changes the state of the dialogue for save/load
				}
				//if pressing will change what the press dialogue is on future attempts to press, it changes it here.
				if (diag.Value.getNewPress() != null)								//checks for any new Press Dialogue
				{
					diag.Value.setPress(current.getNewPress());						//sets the press to the new target.
					diag.Value.setState(1);											//changes the state of the dialogue for save/load
				}
			}
			i++;																	//moves the counter forward
		}
		//cycles through the inventory to confirm that you have the same items you were supposed to have
		foreach (KeyValuePair<string, ItemScript> item in inventoryList) 
		{
			if (mySaveGame2.saveStates[i] == '1')		//adds the item to your inventory
				item.Value.setOwned(true);
			else
				item.Value.setOwned (false);			//removes it from your inventory
			i++;
		}

		
		current = dialogueTree [mySaveGame2.current];	//marks the current dialogue to match where you left off.

		clearText ();
	}

	//a script that wipes out the current text to prepare it for the next.
	void clearText()
	{
		diagPause = true;				//turns on diagPause to prevent multiple commands from a single key stroke
		selectedItem = null;			//deselects any selected items
		presentEvidence = false;		//switches to presentation mode
		sortInventory ();				//hides the inventory
		str = "";						//empties the text body.
		diagCounter = 0;				//moves the counter back to the begining so it can feed in the new text
	}

	void OnGUI() 
	{
		//interface details
		GUI.skin.button.wordWrap = true;	//word wrap
		GUI.skin.box.wordWrap = true;
		GUI.skin.box.alignment = TextAnchor.UpperLeft;

		//save and load feautres
		if(!gameOver)			//makes sure you can't save it while you're in a game over screen.
		if (GUI.Button (new Rect (Screen.width * 1 / 10, Screen.height * 1 / 10, 50, 30), "SAVE")) 
		{
			SavingGame();
		}
		if (GUI.Button (new Rect (Screen.width * 1 / 10 + 60, Screen.height * 1 / 10, 50, 30), "LOAD")) 
		{
			LoadingGame ();
		}

		//health bar
		healthBarLength = (Screen.width / 3) * (hitPoints/MAXHITPOINTS);
		GUI.Box(new Rect(Screen.width * 1 / 10 + 150, Screen.height * 1 / 10, healthBarLength, 30), hitPoints + "/" + MAXHITPOINTS);

		//hit points remaining.
		//checks if evidence should be displayed
		if (presentEvidence)
			displayEvidence ();

		//this checks to make sure that the skip forward button has been released, this avoids automatically jumping forward unintentionally.
		if (!Input.GetKey (keyNext) && !Input.GetKey (keyPrev) && !Input.GetKey (keyBack) && !Input.GetKey (keyEvidence))
			diagPause = false;

		strComplete = current.getContent();	//gets the text for the current line of dialogue

		//displays dialogue information
		GUI.Box(new Rect(Screen.width*1/10,Screen.height*4/5-30,120,30),current.getSpeaker());						//lists the name of the speaker
		GUI.Box(new Rect(Screen.width*1/10+120,Screen.height*4/5-30,120,30),"(" + current.getEmotion() + ")");		//lists the current emotion (this will be removed once graphics are implimented
		GUI.Box(new Rect(Screen.width*1/10,Screen.height*4/5,Screen.width*4/5,Screen.height*1/5),str);				//creates the body of the text

		//if an item was just added, it displays the text telling you this.
		if (itemString != "")
			GUI.Box(new Rect(Screen.width*1/10,Screen.height*2/5,Screen.width*4/5,Screen.height*1/5),itemString);				//creates the body of the text


		//this portion is what allows the text to move in a single letter at a time.  There are commands to speed up, skip forward, or skip backwards
		if (diagCounter < strComplete.Length) 
		{
			//if either keyNext or KeyPrev are pressed, it instantly completes the current text. diagPause is used to make sure it doesn't instantly skip when using keys to move from one dialogue to another.
			if (!diagPause && (Input.GetKeyDown (keyNext) || Input.GetKeyDown (keyPrev)))
			{
				diagPause = true;	//sets diagPause to true to prevent multiple commands from being interpretted from one key stroke.
				str = strComplete;	//sets the current string to be the complete text
				diagCounter = strComplete.Length;	//moves the diagCounter to the end to trigger the butons
			}
			//when the diagTimer reaches zero, it adds the next character and resets the timer.
			else if (diagTimer < 0) 
			{
				str += strComplete [diagCounter];	//adds the next character in the string
				diagTimer = DIAGDELAY;				//resets the diagTimer back to the constant DIAGDELAY
				diagCounter++;						//moves the diagCounter forward so it will add the next character next time.
			} 
			//counts down the diagTimer until the next character.
			else 
			{
				//if the keyFast button is pressed, it moves faster based on the DIAGFAST constnant and the deltaTime000.
				if (Input.GetKey (keyFast))
					diagTimer -= DIAGFAST * Time.deltaTime;
				//otherwise it just ticks down based on the deltaTime.
				else
					diagTimer -= Time.deltaTime;
			}
		} 
		//when the current text is complete, options to move through the dialogue will appear based on the values in the current Dialogue
		else
		{
			//if there is a previous Dialogue you are allowed to return to, it will giveyou the previous button.
			if (current.getPrev() != null)
			{
				//either clicking the button or pushing the keyPrev key will let you move back
				if (!diagPause && (GUI.Button(new Rect(Screen.width*9/10,Screen.height*4/5+30,50,30),"PREV")|| Input.GetKeyDown(keyPrev)))
				{
					clearText ();
					current = current.getPrev ();	//moves the current Dialogue back to the previous Dialogue
				}
			}
			//if there is a following Dialogue, it will give you the next button.
			if (current.getNext() != null)
			{
				//either clicking the button or pushing the keyNext key will let you move back
				if (!diagPause && (GUI.Button(new Rect(Screen.width*9/10 + 50,Screen.height*4/5+30,50,30),"NEXT") || Input.GetKeyDown(keyNext)))
				{
					//if there is no current item to gain or lose, it just goes to the next dialogue and clears out the itemString
					if (current.getItem() == null && current.getLose() == null)
					{
						clearText ();
						current = current.getNext ();	//moves the current Dialogue up to the next Dialogue
						itemString = "";				//wipes it out so it doesn't keep telling you you just got an item
					}
					//if the dialogue has an item, it's moved into your inventory and announced, but the dialogue doesn't move forward.
					else if (current.getItem () != null)
					{
						selectedItem = null;			//deselects any selected items
						presentEvidence = false;		//switches to presentation mode
						diagPause = true;					//turns on diagPause to prevent multiple commands from a single key stroke
						inventoryList[current.getItem ()].setOwned(true);							//markes the item as owned.
						sortInventory();					//sorts inventory
						itemString = current.getItem () + " was added to your inventory.";	//displays that this item was added to your inventory
						current.setItem (null);				//removes the item from this dialogue to prevent it from getting added multiple times
					}
					//if the dialogue has an item to be removed, it's removed from your inventory and announced, but the dialogue doesn't move forward.
					else if (current.getLose () != null)
					{
						selectedItem = null;			//deselects any selected items
						presentEvidence = false;		//switches to presentation mode
						diagPause = true;					//turns on diagPause to prevent multiple commands from a single key stroke
						inventoryList[current.getLose()].setOwned(false);							//markes the item as not owned.
						sortInventory();					//sorts inventory
						itemString = current.getLose () + " was removed from your inventory.";	//displays that this item was added to your inventory
						current.setLose (null);				//removes the item from this dialogue to prevent it from getting added multiple times
					}
				}
			}
			//if there is not a following Dialogue, then the next button still appears, and allows you to either end the dialogue or repeat it based on the loop command.
			else
			{
				//either clicking the button or pushing the keyNext key will let you move back
				if (!gameOver && !diagPause && (GUI.Button(new Rect(Screen.width*9/10 + 50,Screen.height*4/5+30,50,30),"END!") || Input.GetKeyDown(keyNext)))
				{
					//if there is a return
					if (hitPoints <= 0)
					{
						clearText ();
						gameOver = true;				//marks the game as over.
						current = GameOver;		//moves current Dialogue back to the last Dialogue Tree
						returnDialogue = null;			//removes the return Dialogue
					}
					else if (returnDialogue != null)
					{
						clearText ();
						current = returnDialogue;		//moves current Dialogue back to the last Dialogue Tree
						returnDialogue = null;			//removes the return Dialogue
					}
					//if loop is true, it will start the dialogue tree over agin.
					else if (loop == true)
					{
						clearText ();
						current = start;				//moves current Dialogue back to the start of the Dialogue Tree
					}
					//if there is a next dialogue tree to go to, set up that.
					else if (nextTree != "")
					{
						clearText ();
						LoadDiag (nextTree);			//loads the next dialogue tree
					}
				}
			}
			//if there is a Press Dialogue, it will give you the Press button.
			if (current.getPress() != null)
			{
				//either clicking the button or pushing the keyPres key will let you press for the alternate dialogue
				if (!diagPause && !presentEvidence && (GUI.Button(new Rect(Screen.width*9/10+25,Screen.height*4/5,60,30),"PRESS") || Input.GetKeyDown(keyPress)))
				{
					clearText ();
					Dialogue tempDiag = current.getPress ();	//stores the press Dialogue in a temp file, as values may change at this point.

					//if pressing will add a new dialogue into the tree, it does so here.
					if (current.getNewNext() != null)
					{
						current.getNext().setPrev(current.getNewNext());				//sets the next Dialogue's prev to link up to the added Dialogue
						current.setNext(current.getNewNext());							//sets the current Dialogue's next to link to the added dialogue.
						current.setState(1);											//changes the state of the dialogue for save/load
					}
					//if pressing will change what the press dialogue is on future attempts to press, it changes it here.
					if (current.getNewPress() != null)
					{
						current.setPress(current.getNewPress());						//sets the press to the new target.
						current.setState(1);											//changes the state of the dialogue for save/load
					}
					current = tempDiag;													//sets current to the original press Dialogue, regardless of any changes
				}
				//either clicking the button or pushing the keyEvidence key will let you view evidence.
				if (!diagPause && !presentEvidence  && (GUI.Button(new Rect(Screen.width*9/10+10,Screen.height*4/5+60,85,30),"EVIDENCE") || Input.GetKeyDown(keyEvidence)))
				{
					diagPause = true;				//turns on diagPause to prevent multiple commands from a single key stroke
					presentEvidence = true;			//switches to presentation mode
				}
				//either clicking the button or pushing the keyBack key will return to the cros examination.
				if (!diagPause && presentEvidence && (GUI.Button(new Rect(Screen.width*9/10+25,Screen.height*4/5,60,30),"BACK") || Input.GetKeyDown(keyBack)))
				{
					diagPause = true;				//turns on diagPause to prevent multiple commands from a single key stroke
					presentEvidence = false;		//switches out of presentation mode
					selectedItem = null;			//deselects the selectedItem
					sortInventory();				//hide the inventory
				}
				//either clicking the button or pushing the keyBack key will present the evidence.
				if (!diagPause && presentEvidence && !diagPause && selectedItem != null && (GUI.Button(new Rect(Screen.width*9/10+10,Screen.height*4/5+60,80,30),"PRESENT") || Input.GetKeyDown(keyPresent)))
				{
					presentEvidence = false;		//switches to presentation mode
					if (current.checkEvidence(selectedItem.caseID))	//checks if this evidence and this dialogue advance the story.
					{
						loop = false;					//breaks the loop
						selectedItem = null;			//deselects the selectedItem
						LoadDiag (nextTree);			//loads the next Dialogue Pass
					}
					//otherwise return a negative statement
					else
					{
						returnDialogue = current;	//stores the current dialogue in the return Dialogue to be returned to later.
						selectedItem = null;			//deselects the selectedItem
						hitPoints -= DAMAGE;			//you lose hit points for presenting bad evidence
						current = startFail[rnd.Next(startFail.Count)];		//sets the current to the first fail dialogue
					}
					clearText ();											//removes the text for new text to come in
				}
			}
		}
	}
	

	//loads in a new set of Dialogues and sets them to the dialogueTree dictionary, creating the linked list as well.
	void LoadDiag(string destination)
	{
		//stores the current destination as thisTree for save/load purposes
		thisTree = destination;

		//wipes out the current filename path
		string filename = null;
		//wipes out the existing information to make a new one.
		start = null;					// the starting item for the dialogue.
		current = null;					// keeps track of which dialogue is currently being dispalayed
		returnDialogue = null;			// a temprary value used to keep track of where the dialogue was when left (at this point, only used for presenting failed evidence)
		startFail = new List<Dialogue>();///an array holding the beginnings of failure dialogues to be pulle dout reandomly when necessary.
		GameOver = null;				//the start of the game over dialogue path
		dialogueTree = new Dictionary<string,Dialogue>();	//the dictionary that holds the items for the dialogue Tree/LinkedList list
		nextTree = "";					//the location of the next file to read.

		//sets it based on the incoming destination information.
		filename = Application.dataPath + destination;

		//checks to make sure it exists.
		if (!File.Exists(filename))
		{
			print("Could not open file: " + filename);	//error message.  This should never actually be seen.
			return;
		}

		//reads the file into a StreamReader.
		StreamReader sr  = new StreamReader (filename);

		//stores the text file by individual lines, using the standard line break to parse it
		string[] statData = sr.ReadToEnd().Split(LINE_DELIM, System.StringSplitOptions.RemoveEmptyEntries);

		//temporary pointers to help sort the Dialogues and how they connext.
		Dialogue currDiag = null;				//the most recently called primary Dialogue
		Dialogue currPress = null;				//the most recently called Press Dialogue
		Dialogue currDiagAdded = null;			//the most recently called Dialgoue that gets added to the primary path after a trigger.
		Dialogue currPressAdded = null;			//the most recently called Press Dialogue that gets added to the primary path after a trigger
		Dialogue currPressAlt = null;			//the most recently called Press Dialogue that replaces an existing Press Dialogue after a trigger.
		Dialogue currFail = null;				//the most recently called Failure Dialogue.
		Dialogue currGameOver = null;			//the most recently called Game Over Dialogue
		string currTarget = null;				//a string identifying what type of Dialogue has been most recently called, to assign sub variables to the right one of the Dialogues above.

		//reads through each of the individual lines to parse them further
		//********************************************************************
		//Breakdown of the individual identifiers.
		//**Dialogue identifiers create new Dialogues and then assign them to the Linked List, based on what kind they are.
		//Both "Diag" and "Cross" are basic Dialogues and create the main dialogue tree.
		//"CrossAdded" is when there's a standard Dialogue, but it is not initially part of the tree, until such time as it gets added into it by a trigger.
		//"Press" denotes that this Dialogue is accessed through the Press command, rather than the usual back and foward chain.
		//"PressAdded" is for a Press Dialogue that gets attached to a "CrossAdded", so it won't show up until the CrossAdded does.
		//"PressAlt" is for when a Press Dialogue gets replaced by a new one.  This is typically done through the same trigger that adds a new main Dialogue
		//"NewF" clears out the fail dialogue to start a new one.
		// "Fail" is for a dialogue presented when evidence fails. Unlike most dialogues, it is not part of the regular chain.
		// "GameOver" is for a dialogue presented when you run out of tries. Unlike most dialogues, it is not part of the regular chain.
		//**detail identifiers are information that needs to be displayed, such as the speaker, the emotion, and the body of text
		//all of them use the CurrTarget to make sure they store the information on the right Dialogue object.
		//"Name" identifies who the speaker is. Currently a String, will eventually be swapped out with a pointer.
		//"Text" identifies the dialogue that will be printed on the screen.
		//"Emot" identifies what the emotion being portrayed is.  Eventually it will cause the graphics/animations of the characters to change, but right
		//now it merely displays the emotion on the screen.
		//** "Corr" marks the case ID of the correct evidence.  It is checked against SelectedItem's caseID to see if they match
		//**"Item" provides the key to an item in the inventory to be triggered later.
		//**"Lose" provides the key to an item in the inventory that will be removed later.
		//**"Loop" marks whether the content runs in a loop or not.  Typically Cross Examines loop, while regular conversations do not.
		//**"Next" stores the location of the next cross examination or marks where it moves to a different event.
		//********************************************************************

		foreach (string section in statData)
		{
			section.Trim();				//just cleans out any stray characters that could complicate things.

			//each line is broken into the label and the body and then read appropriately using a switch case.
			string[] lineData = section.Split(LABEL_DELIM, System.StringSplitOptions.RemoveEmptyEntries);

			//the switch looks at each label, and acts accordingly.  This is split into Scene (n/a), Diaglogue identifiers, detail identifiers, new evidence, and the loop and next tree.
			switch(lineData[0])
			{
			//"Scene" is strictly there for filling purpopses and is removed without adding anything.
			case ("Scene"):
				break;
			//**Dialogue identifiers create new Dialogues and then assign them to the Linked List, based on what kind they are.
			//Both "Diag" and "Cross" are basic Dialogues and create the main dialogue tree.
			case ("Diag"):
			case ("Cross"):
				currTarget = "Diag";							//identifies the current Target for the details later
				dialogueTree[lineData[1].Trim()] = new Dialogue(true, lineData[1].Trim());	//creates a new Dialogue
				//if this is the not the first Dialogue, it connects to the previous one.
				if (currDiag != null)					
				{
					currDiag.setNext(dialogueTree[lineData[1].Trim()]);	//links the Next of the last Dialogue to this one.
					dialogueTree[lineData[1].Trim()].setPrev(currDiag);	//links the Prev of this Dialogue to the last one.
				}
				//if there is an existing Press Dialogue, this also gets connected. Note a Press Dialgoue links to the next main Dialogue, but not vice versa 
				if (currPress != null)
				{
					currPress.setNext(dialogueTree[lineData[1].Trim()]);	//links the Next of the last Press Dialogue to this one.
				}
				//similarly if there is an existing Added Press, it will also link to the next one.
				if (currPressAdded != null)
				{
					currPressAdded.setNext(dialogueTree[lineData[1].Trim()]);//links the Next of the last Press Dialogue to this one.
				}
				//similarly if there is an existing Added Press, it will also link to the next one.
				if (currPressAlt != null)
				{
					currPressAlt.setNext(dialogueTree[lineData[1].Trim()]);//links the Next of the last Press Dialogue to this one.
				}
				//if thre's an existing Added Dialogue, this gets added too.  Note the current Dialogue doesn't link back yet.  That gets added during the trigger.
				if (currDiagAdded != null)
				{
					currDiagAdded.setNext(dialogueTree[lineData[1].Trim()]);//links the next of the Added Dialogue to this one.
				}
				//if the start hasn't been assigned, then this is the first dialogue and gets added as one.
				if (start == null)
				{
					start = dialogueTree[lineData[1].Trim()];		//links the start to the current tree.
				}
				currDiag = dialogueTree[lineData[1].Trim()];		//this Dialogue now replaces the last one as the currDiag
				//all previous connections are cut at this point, so the other pointers are wiped out.
					currPress = null;
					currPressAdded = null;
					currPressAlt = null;
					currDiagAdded = null;
				break;
			//"CrossAdded" is when there's a standard Dialogue, but it is not initially part of the tree, until such time as it gets added into it by a trigger.
			case ("CrossAdded"):
				currTarget = "DiagAdded";							//identifies the current Target for the details later
				dialogueTree[lineData[1].Trim()] = new Dialogue(true, lineData[1].Trim());	//creates a new Dialogue
				//if this is the not the first Dialogue, (and it never should be) it connects to the previous one.
				if (currDiag != null)					
				{
					dialogueTree[lineData[1].Trim()].setPrev(currDiag);	//links the prev of this Dialogue to the previous Dialogue.
					currDiag.setNewNext(dialogueTree[lineData[1].Trim()]);	//links the newNext of the previous Dialogue to this one.
				}
				//if there is a previous Press Dialogue, it gets linked to that one. As usual, the Press links to the Dialogue, but it does not link back.
				if (currPress != null)
				{
					currPress.setNext(dialogueTree[lineData[1].Trim()]);	//links the next of the current Press Dialogue to this one.
				}
				//similarly if there is an existing Added Press, it will also link to the next one.
				if (currPressAdded != null)
				{
					currPressAdded.setNext(dialogueTree[lineData[1].Trim()]);//links the Next of the last PressAdded Dialogue to this one.
				}
				//similarly if there is an existing Added Press, it will also link to the next one.
				if (currPressAlt != null)
				{
					currPressAlt.setNext(dialogueTree[lineData[1].Trim()]);//links the Next of the last PressAlt Dialogue to this one.
				}
				currDiagAdded = dialogueTree[lineData[1].Trim()];	//this Dialogue is now the currDiagAdded
				//All previous Press Dialogues are cut at this point, so the other pointers are wiped out.
					currPress = null;
					currPressAlt = null;
				break;
			//"Press" denotes that this Dialogue is accessed through the Press command, rather than the usual back and foward chain.
			case ("Press"):
				currTarget = "Press";								//identifies the current Target for the details later
				dialogueTree[lineData[1].Trim()] = new Dialogue(true, lineData[1].Trim());	//creates a new Dialogue
				//if there is a previous Press, than the two link together similar to how the main tree does.
				if (currPress != null)
				{
					currPress.setNext(dialogueTree[lineData[1].Trim()]);	//links the next of the previous Press Dialogue to this one.
					dialogueTree[lineData[1].Trim()].setPrev(currPress);	//links the prev of this Dialogue to the previous Press one.
				}
				//otherwise, it connects to the existing main Dialogue tree. Note, the Press does not link back to the main Dialogue, you need to end the Press chain to return to the main List.
				else if (currDiag != null)
				{
					currDiag.setPress(dialogueTree[lineData[1].Trim()]);	//links the press of the current main Dialogue to this one.
				}
				//otherwise it reports an error
				else
					throw new System.ArgumentException ("No interrogate to link press to: " + dialogueTree[lineData[1].Trim()]);
				currPress = dialogueTree[lineData[1].Trim()];				//sets this Dialogue to be the current Press.
				break;
			//"PressAdded" is for a Press Dialogue that gets attached to a "CrossAdded", so it won't show up until the CrossAdded does.
			case ("PressAdded"):
				currTarget = "PressAdded";							//identifies the current Target for the details later
				dialogueTree[lineData[1].Trim()] = new Dialogue(true, lineData[1].Trim());	//creates a new Dialogue
				//if there is a previous PressAdded, than the two link together similar to how the main tree does.
				if (currPressAdded != null)
				{
					currPressAdded.setNext(dialogueTree[lineData[1].Trim()]);	//links the next of the previous PressAdded Dialogue to this one.
					dialogueTree[lineData[1].Trim()].setPrev(currPressAdded);	//links the prev of this Dialogue to the previous PressAdded one.
				}
				//otherwise it gets attached to the CrossAdded Dialogue.Note, the Press does not link back to the main Dialogue, you need to end the Press chain to return to the main List.
				else if (currDiagAdded != null)
				{
					currDiagAdded.setPress(dialogueTree[lineData[1].Trim()]);	//links the press of the previous Dialogue to this one.
				}
				currPressAdded = dialogueTree[lineData[1].Trim()];			//sets this Dialogue to be the new current PressAdded
				break;
			//"PressAlt" is for when a Press Dialogue gets replaced by a new one.  This is typically done through the same trigger that adds a new main Dialogue
			case ("PressAlt"):
				currTarget = "PressAlt";							//identifies the current Target for the details later
				dialogueTree[lineData[1].Trim()] = new Dialogue(true, lineData[1].Trim());	//creates a new Dialogue
				//if there is a previous PressAlt, than the two link together similar to how the main tree does.
				if (currPressAlt != null)
				{
					currPressAlt.setNext(dialogueTree[lineData[1].Trim()]);	//links the next of the previous PressAlt Dialogue to this one.
					dialogueTree[lineData[1].Trim()].setPrev(currPressAlt);	//links the prev of this Dialogue to the previous PressAlt one.
				}
				//This will be assigned to the newPress of the current dialogue tree for when it's needed by the trigger
				else if (currDiag != null)					
				{
					currDiag.setNewPress(dialogueTree[lineData[1].Trim()]);	//links the newPress of the previous Dialogue to this one.
				}
				currPressAlt = dialogueTree[lineData[1].Trim()];			//sets this Dialogue to be the new current PressAlt
				break;
			//"NewF" clears out the fail dialogue to start a new one.
			case("NewF"):
				currFail = null;
				break;
			// "Fail" is for a dialogue presented when evidence fails. Unlike most dialogues, it is not part of the regular chain.
			case ("Fail"):
				currTarget = "Fail";							//identifies the current Target for the details later
				dialogueTree[lineData[1].Trim()] = new Dialogue(true, lineData[1].Trim());	//creates a new Dialogue
				//if this is the not the first Dialogue, it connects to the previous one. Unlike most dialogue, it only goes forward
				if (currFail != null)					
				{
					currFail.setNext(dialogueTree[lineData[1].Trim()]);	//links the Next of the last Dialogue to this one.
				}
				//if it is the first dialogue, it gets sent to the startFail dialogue tree, to be pulled out randomly.
				else
				{
					startFail.Add(dialogueTree[lineData[1].Trim()]);
				}
				currFail = dialogueTree[lineData[1].Trim()];		//this Dialogue now replaces the last one as the currFail
				//all previous connections are cut at this point, so the other pointers are wiped out.
				currPress = null;
				currPressAdded = null;
				currDiagAdded = null;
				break;
			// "GameOver" is for a dialogue presented when you run out of tries. Unlike most dialogues, it is not part of the regular chain.
			case ("GameOver"):
				currTarget = "GameOver";							//identifies the current Target for the details later
				dialogueTree[lineData[1].Trim()] = new Dialogue(true, lineData[1].Trim());	//creates a new Dialogue
				//if this is the not the first Dialogue, it connects to the previous one. Unlike most dialogue, it only goes forward
				if (currGameOver != null)					
				{
					currGameOver.setNext(dialogueTree[lineData[1].Trim()]);	//links the Next of the last Dialogue to this one.
				}
				//if it is the first dialogue, it gets sent to the startFail dialogue tree, to be pulled out randomly.
				else
				{
					GameOver = dialogueTree[lineData[1].Trim()];
				}
				currGameOver = dialogueTree[lineData[1].Trim()];		//this Dialogue now replaces the last one as the currFail
				//all previous connections are cut at this point, so the other pointers are wiped out.
				currFail = null;
				currPress = null;
				currPressAdded = null;
				currDiagAdded = null;
				break;
			//**detail identifiers are information that needs to be displayed, such as the speaker, the emotion, and the body of text
			//all of them use the CurrTarget to make sure they store the information on the right Dialogue object.
			//"Name" identifies who the speaker is. Currently a String, will eventually be swapped out with a pointer.
			case ("Name"):
				if (currTarget == "DiagAdded")
					currDiagAdded.setSpeaker (lineData[1].Trim());		//adds the text to the CrossAdded speaker string.
				else if (currTarget == "PressAdded")
					currPressAdded.setSpeaker (lineData[1].Trim());	//adds the text to the PressAdded speaker string.
				else if (currTarget == "PressAlt")
					currPressAlt.setSpeaker (lineData[1].Trim());		//adds the text to the PressAlt speaker string.
				else if (currTarget == "Press")
					currPress.setSpeaker (lineData[1].Trim());			//adds the text to the Press speaker string
				else if (currTarget == "Fail")
					currFail.setSpeaker (lineData[1].Trim());			//adds the text to the Press speaker string
				else if (currTarget == "GameOver")
					currGameOver.setSpeaker (lineData[1].Trim());			//adds the text to the Game Over speaker string
				else
					currDiag.setSpeaker (lineData[1].Trim());			//adds the text to the main Dialogue speaker string (default)
				break;
			//"Text" identifies the dialogue that will be printed on the screen.
			case ("Text"):
				if (currTarget == "DiagAdded")
					currDiagAdded.setContent (lineData[1].Trim());		//adds the text to the CrossAdded content string.
				else if (currTarget == "PressAdded")
					currPressAdded.setContent (lineData[1].Trim());	//adds the text to the PressAdded content string.
				else if (currTarget == "PressAlt")
					currPressAlt.setContent (lineData[1].Trim());		//adds the text to the PressAlt content string.
				else if (currTarget == "Press")
					currPress.setContent (lineData[1].Trim());			//adds the text to the Press content string.
				else if (currTarget == "Fail")
					currFail.setContent (lineData[1].Trim());			//adds the text to the Press speaker string
				else if (currTarget == "GameOver")
					currGameOver.setContent (lineData[1].Trim());			//adds the text to the Game Over content string
				else
					currDiag.setContent (lineData[1].Trim());			//adds the text to the main Dialogue content string (default)
				break;
			//"Emot" identifies what the emotion being portrayed is.  Eventually it will cause the graphics/animations of the characters to change, but right
			//now it merely displays the emotion on the screen.
			case ("Emot"):
				if (currTarget == "DiagAdded")
					currDiagAdded.setEmotion (lineData[1].Trim());		//adds the text to the CrossAdded emotion string.
				else if (currTarget == "PressAdded")
					currPressAdded.setEmotion (lineData[1].Trim());	//adds the text to the PressAdded emotion string.
				else if (currTarget == "PressAlt")
					currPressAlt.setEmotion (lineData[1].Trim());		//adds the text to the PressAlt emotion string.
				else if (currTarget == "Press")
					currPress.setEmotion (lineData[1].Trim());			//adds the text to the Press emotion string.
				else if (currTarget == "Fail")
					currFail.setEmotion (lineData[1].Trim());			//adds the text to the Press speaker string
				else if (currTarget == "GameOver")
					currGameOver.setEmotion (lineData[1].Trim());			//adds the text to the Game Over emotion string
				else
					currDiag.setEmotion (lineData[1].Trim());			//adds the text to the main Dialogue emotion string (default)
				break;
			//** "Corr" marks the case ID of the correct evidence.  It is checked against SelectedItem's caseID to see if they match
			case("Corr"):
				if (currTarget == "DiagAdded")
					currDiagAdded.setEvidence(lineData[1].Trim());
				else
					currDiag.setEvidence(lineData[1].Trim());
				break;
			//**"Item" provides the key to an item in the inventory to be triggered later.
			case("Item"):
					//adds it to the most recently generated dialogue.
				if (currTarget == "DiagAdded")
					currDiagAdded.setItem(lineData[1].Trim());			//adds the item to the CrossAdded.
				else if (currTarget == "PressAdded")
					currPressAdded.setItem(lineData[1].Trim());			//adds the item to the PressAdded.
				else if (currTarget == "PressAlt")
					currPressAlt.setItem(lineData[1].Trim());				//adds the item to the PressAlt.
				else if (currTarget == "Press")
					currPress.setItem(lineData[1].Trim());				//adds the item to the Press.
				else if (currTarget == "Fail")
					currFail.setItem(lineData[1].Trim());					//adds the item to the Press
				else if (currTarget == "GameOver")
					currGameOver.setItem (lineData[1].Trim());	//adds the text to the Game Over.
				else
					currDiag.setItem(lineData[1].Trim());					//adds the item to the main Dialogue emotion string (default)
				break;
			//**"Lose" provides the key to an item in the inventory that will be removed later.
			case("Lose"):
				//adds it to the most recently generated dialogue.
				if (currTarget == "DiagAdded")
					currDiagAdded.setLose(lineData[1].Trim());			//adds the item to the CrossAdded.
				else if (currTarget == "PressAdded")
					currPressAdded.setLose(lineData[1].Trim());			//adds the item to the PressAdded.
				else if (currTarget == "PressAlt")
					currPressAlt.setLose(lineData[1].Trim());				//adds the item to the PressAlt.
				else if (currTarget == "Press")
					currPress.setLose(lineData[1].Trim());				//adds the item to the Press.
				else if (currTarget == "Fail")
					currFail.setLose(lineData[1].Trim());					//adds the item to the Press
				else if (currTarget == "GameOver")
					currGameOver.setLose (lineData[1].Trim());	//adds the text to the Game Over.
				else
					currDiag.setLose(lineData[1].Trim());					//adds the item to the main Dialogue emotion string (default)
				break;
			//**"Loop" marks whether the content runs in a loop or not.  Typically Cross Examines loop, while regular conversations do not.
			case ("Loop"):
				if (lineData[1].Trim () == "Yes")
					loop = true;								//sets loop to true
				else
					loop = false;								//sets loop to false
				break;
			//**"Next" stores the location of the next cross examination or marks where it moves to a different event.
			case("Next"):
				nextTree = lineData[1].Trim ();
				break;
			}
		}
		current = start;										//sets the current text being displayed to the start of the tree.

	}

	//loads in a masterlist of all the items you can have.
	void LoadInventory(string destination)
	{
		thisInventory = destination;							//stores the current location for future information

		
		//wipes out the current filename path
		string filename = null;

		//wipes out the existing information to make a new one.
		foreach (KeyValuePair<string, ItemScript> item in inventoryList) 
		{
			Destroy(item.Value.gameObject);						//kills the existing objects to avoid duplicates
		}
		inventoryList = new Dictionary<string,ItemScript>();	//the dictionary that holds the items for the dialogue Tree/LinkedList list

		//sets it based on the incoming destination information.
		filename = Application.dataPath + destination;
		
		//checks to make sure it exists.
		if (!File.Exists(filename))
		{
			print("Could not open file: " + filename);	//error message.  This should never actually be seen.
			return;
		}
		
		//reads the file into a StreamReader.
		StreamReader sr  = new StreamReader (filename);
		
		//stores the text file by individual lines, using the standard line break to parse it
		string[] statData = sr.ReadToEnd().Split(LINE_DELIM, System.StringSplitOptions.RemoveEmptyEntries);
		
		//temporary pointers to help sort the Dialogues and how they connext.
		ItemScript currItem = null;				//the most recently called item.

		//reads through each of the individual lines to parse them further
		foreach (string section in statData)
		{
			section.Trim();				//just cleans out any stray characters that could complicate things.
			
			//each line is broken into the label and the body and then read appropriately using a switch case.
			string[] lineData = section.Split(LABEL_DELIM, System.StringSplitOptions.RemoveEmptyEntries);
			
			//the switch looks at each label, and acts accordingly.  This is split into Scene (n/a), Diaglogue identifiers, detail identifiers, new evidence, and the loop and next tree.
			switch(lineData[0])
			{
				//"Case" is strictly there for filling purpopses and is removed without adding anything.
			case ("Case"):
				//similarly, "Note" is there to add information and comments to the file.
			case ("Note"):
				break;
				//**Dialogue identifiers create new Dialogues and then assign them to the Linked List, based on what kind they are.
				//Both "Diag" and "Cross" are basic Dialogues and create the main dialogue tree.
			case ("Name"):
				currItem = Instantiate (item, new Vector3 (0, 0, 0), Quaternion.identity) as ItemScript;			//generates the item
				currItem.name = lineData[1].Trim();																	//sets the name
				inventoryList[currItem.name] = currItem;															//stores it in the Dictionary
				break;
			case ("ID"):
				currItem.setCaseID (lineData[1].Trim ());
				break;
			case ("Descript"):
				currItem.setDescript (lineData[1].Trim ());
				break;
			case ("Evid"):
				if (lineData[1].Trim().ToLower() == "yes")
				currItem.setEvidence(true);
				break;
			case ("Owned"):
				if (lineData[1].Trim().ToLower() == "yes")
					currItem.setOwned (true);
				break;
			}
		}
		current = start;										//sets the current text being displayed to the start of the tree.
		sortInventory ();										//sort the inventory

	}


	//display all the evidence that can be presented.
	public void displayEvidence()
	{
		//checks each item in the list and sorts it to the right location.
		foreach (KeyValuePair<string, ItemScript> item in inventoryList)
		{
			Vector3 V = Camera.main.WorldToScreenPoint(item.Value.transform.position);		//marks it's physical location to assist the button locations.
			if (item.Value.isEvidence() && item.Value.isOwned ())							//checks to make sure the item is evidence and not just an item.
			{
				item.Value.gameObject.SetActive (true);										//unhides it if it's evidence.
				if (GUI.Button(new Rect(V.x,Screen.height - V.y+10,100,20),item.Value.name))	//displayes a button with the evidence name 
				{
					selectedItem = item.Value;												//selects the evidence if the button is pushed.
				}
			}
			else
			{
				item.Value.gameObject.SetActive (false);										//none evidence is deactivated.
			}
		}

		if (selectedItem != null)
		{
			GUI.Box(new Rect(Screen.width*1/10,Screen.height*4/5-90,Screen.width*4/5,60),selectedItem.getDescription());						//lists the details of the item

		}

	}


	public void sortInventory()
	{
		//constants to make sure the items are in the right locations
		const int XSPACE = 3;
		const int YSPACE = -3;
		int XMAX = 3;
		int XMIN = -3;
		int YMIN = 3;

		//variables to make sure the items are in the right locations
		int x = XMIN;
		int y = YMIN;
		int z = 0;


		foreach (KeyValuePair<string, ItemScript> item in inventoryList)
		{
			if (item.Value.isOwned() && item.Value.isEvidence())					//only items you own are sorted, this avoids gaps
			{
				item.Value.transform.localPosition = new Vector3(x,y,z);		//moves the item to the currently marked location
				x += XSPACE;													//moves the marked location to the right.
				if (x > XMAX)													//if it gets too far to the right
				{
					x = XMIN;														//it moves back to the far left
					y += YSPACE;												//but moves down
				}
			}
				item.Value.gameObject.SetActive (false);						//hides all items as well.
		}
	}


}