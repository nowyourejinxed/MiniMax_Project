//Sabrina Jackson
using UnityEngine;


public class Slots : MonoBehaviour
{
    public int slotNum;
    public bool player1Slot;
    [SerializeField] private int numSeeds;
    public bool isStore;

    public int Seed
    {
        get
        {
            return numSeeds;
        }
        set
        {
            numSeeds = value;
        }
    }
}
