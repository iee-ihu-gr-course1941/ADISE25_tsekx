using UnityEngine;

// i will use this class to save the data and then send it as a JSON to the server . There are 2 types of scripts , the monobehaviour ones and the data ones , this is a data one.
public class MoveDTO
{
    public int from;
    public int to;
    public int playerId;
}

// there is no need to import this , every .cs file in my project can see it without use the "using" keyword to import it to ther C# files .
