using UnityEngine;
using System.Collections;

public class ItemScript : MonoBehaviour 
{
	public string caseID;		//it's caseID number, for sorting and checks. format is #.#.# which represents Case.Day.ID.  Items for multiple cases or days will be reflected by an X.
	public string descript;		//a description of the item.
	public bool evidence=false;	//is it evidence? Items that are not evidence are hidden during court cases.
	public bool owned = false;	//checks if it's in the player's inventory

	//getters and setters
	public void setCaseID(string ID){caseID = ID;}
	public string getCaseID(){return caseID;}

	public void setDescript(string d){descript = d;}
	public string getDescription(){return descript;}

	public void setEvidence(bool e){evidence = e;}
	public bool isEvidence(){return evidence;}

	public void setOwned(bool o){owned = o;}
	public bool isOwned(){return owned;}

}