using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;

[Serializable]
public class MySaveGame : SaveGame
{
	
	public float hitPoints = 44;						//sets your starting hit points.
	public string thisTree = "";					//stores the text file location of the current tree.
	public string thisInventory = "";				//stores the text file location of theinventory for the current case.
	public string saveStates;						//stores the state of each dialogue in the dialogue tree.
	public string current;							//stores the key for the current dialogue

}