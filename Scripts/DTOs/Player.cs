using UnityEngine;
using System.Collections.Generic;

//namespace DataClasses;
public class Player
{
    public string id;
    public List<Pawn> activePawns = new List<Pawn>();
    public List<Pawn> capturedPawns = new List<Pawn>();

    public Player(string id)
  {
    this.id=id;
  }
}
