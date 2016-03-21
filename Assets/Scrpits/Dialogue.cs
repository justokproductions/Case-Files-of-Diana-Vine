//**********************************************************************
// Name: Dialogue
// Purpose: Dialogue objects create a linked list to allow the user to
//  	navigate through the dialogue trees.
//**********************************************************************

public class Dialogue
{
	public Dialogue next = null;		//the next Dialogue on the list
	public Dialogue prev = null;		//the last Dialogue on the list
	public Dialogue press = null;		//an alternate path on the list when Press is used intead of Next
	public Dialogue newNext = null;		//replaces the next Dialogue on the regular path
	public Dialogue newPress = null;	//replaces the pree Dialogu eonthe press path
	private string key = "";			//holds the Dictionary key for the Dialogue
	private string content;				//contains the actual dialogue being said
	private string speaker;				//lists the person speaking
	private string emotion;				//lists their emotion/animation
	private string evidence;			//if there is evidence to be presented to cause a change, this holds that information.
	private bool testimony = true;		//is this part of a testimony. some aspects act differently if it's not.
	private string item = null;			//the key to the inventoryList dictionary for the item this Dialogue should add to your inventory
	private string lose = null;			//the key to the inventoryList dictionary for the item this Dialogue should remove from your inventory
	private int saveState = 0;			//saves a state of change, used for saveing and loading

	public Dialogue ()
	{
	}

	//constructor
	public Dialogue (bool t, string k)
	{
		testimony = t;
		key = k;
	}


	//checks if the right evidence was presented.  If not, return false.
	public bool checkEvidence (string e)
	{
		if (evidence == e) 
		{
			return true;
		}
		return false;
	}

	//getters and setters
	public void setKey (string k){key = k;}
	public string getKey (){return key;}

	public void setSpeaker (string s){speaker = s;}
	public string getSpeaker (){return speaker;}

	public void setEmotion (string e){emotion = e;}
	public string getEmotion (){return emotion;}

	public void setContent (string c){content = c;}
	public string getContent (){return content;}

	public void setNewNext (Dialogue n){newNext = n;}
	public Dialogue getNewNext (){return newNext;}
	
	public void setNewPress (Dialogue p){newPress = p;}
	public Dialogue getNewPress (){return newPress;}

	public void setNext (Dialogue n){next = n;}
	public Dialogue getNext (){return next;}

	public void setPrev (Dialogue p){prev = p;}
	public Dialogue getPrev (){return prev;}

	public void setPress (Dialogue p){press = p;}
	public Dialogue getPress (){return press;}

	public void setEvidence (string e){evidence = e;}
	public string getEvidence (){return evidence;}

	public void setTestimony (bool t){testimony = t;}
	public bool isTestimony (){return testimony;}

	public void setItem (string i){item = i;}
	public string getItem (){return item;}

	public void setLose (string i){lose = i;}
	public string getLose (){return lose;}

	public void setState (int s){saveState = s;}
	public int getState (){return saveState;}
}