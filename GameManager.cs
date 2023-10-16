//Sabrina Jackson

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityAtoms.BaseAtoms;
using UnityEngine.SceneManagement;


//Game Manager for AI vs Player Mancala Game
public class GameManager : MonoBehaviour
{
    #region Bools
    private bool tieGame = false;
    private bool player1Won;
    private bool isPlayer1Turn = true;
    #endregion

    #region Game Vars
    private Camera mainCam;
    public GameObject board;
    [SerializeField] GameObject piecesPrefab;
    public static GameManager instance;
    private List<SlotCopy> deepboardCopy = new List<SlotCopy>();
    private List<Slots> boardSlots;                               
    private Slots lastSlot;    
    #endregion

    #region Numerical Values
    private float turnDelay;   
    public float stonesSpeed = 0.1f;
    #endregion
    
    #region TMP
    public TextMeshProUGUI score1Gui;
    public TextMeshProUGUI score2Gui;
    public TextMeshProUGUI gameOver;
    public TextMeshProUGUI difficulty;
    public GameObject panel;
    #endregion
    
    #region Audio
    [SerializeField] private AudioClip piecesMoving;
    [SerializeField] private AudioClip winGame;
    [SerializeField] private AudioClip loseGame;
    #endregion
    
    #region Atoms
    [SerializeField] private IntVariable Difficulty;
    [SerializeField] private VoidEvent startOver;
    #endregion

    #region Supplement Classes
    //class to prevent game board from being changed in minimax
    public class SlotCopy
    {
        public int slotNumber;
        public bool player1;
        public int Seeds;
        public bool isaStore;
    }
    
    //Possible moves storing the state of a slot and the index of the slot
    public class PosMove
    {
        public List<SlotCopy> tempCopy = new List<SlotCopy>();
        public int moveNum;
    }
    #endregion
    void Awake()
    {
        instance = this;
    }
    void Start()
    {
        startOver.Register(Restart);
        mainCam = Camera.main;
        Initialize();
        panel.SetActive(false);
        //set display of difficulty level
        switch (Difficulty.Value)
        {
            case 0: difficulty.text = "Easy";
                break;
            case 1: difficulty.text = "Moderate";;
                break;
            case 2: difficulty.text = "Hard";;
                break;
        }
    }

    private void OnDestroy()
    {
        startOver.Register(Restart);
    }

    void Update()
    {
        
        // Check if the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
           // Load the "Intro Scene" when Escape key is pressed
            SceneManager.LoadScene("Intro Scene");
        }
        //play game
        //if player1 turn
        if (Input.GetMouseButtonDown(0) && isPlayer1Turn)
        {
            Ray reference = mainCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(reference, out hit, 100))
            {

                GameObject temp = hit.transform.gameObject;
                if (!temp.GetComponent<SlotSelected>().isaStore)
                {
                   //Debug.Log(hit.transform.gameObject.GetComponent<SlotSelected>().slotNumber);
                    int selectedIndex = temp.GetComponent<SlotSelected>().slotNumber; //need to check conditions, prob a better way to do this
                    if (CanChooseSlot(selectedIndex))
                    {
                        PlayGame(selectedIndex);
                        isPlayer1Turn = false;
                    }
                }

            }
        }
        //if AI turn
        if (!isPlayer1Turn)
        {
            deepboardCopy.Clear();
            //create copy of current board state
            foreach (var s in boardSlots)
            {
                SlotCopy newSlot = new SlotCopy();
                newSlot.player1 = s.player1Slot;
                newSlot.slotNumber = s.slotNum;
                newSlot.isaStore = s.isStore;
                newSlot.Seeds = s.Seed;
                deepboardCopy.Add(newSlot);
            }
            int move;
            move = AI(deepboardCopy);
            StartCoroutine(MovePieces(move));
            CheckWinCon();
            isPlayer1Turn = true;
        }
        
    }

    //resets the game state and adjusts the value of difficulty depending on previous games
    void Restart()
    {
        //reset board
        CreateBoard();
        foreach (var slot in boardSlots)
        {
            Seeds[] pieces = slot.transform.GetComponentsInChildren<Seeds>();
            foreach (Seeds seed in pieces)
            {
                seed.transform.position = Vector3.one * 150;
            }
        }
        StartCoroutine(InitializePieces());
        panel.SetActive(false);
        //adjust difficulty
        if (player1Won)
        {
            if (Difficulty.Value < 2)
            {
                Difficulty.Value++;
            }
        }
        else
        {
            if (Difficulty.Value > 0)
            {
                Difficulty.Value -= 1;
            }
        }
        //display difficulty and points
        switch (Difficulty.Value)
        {
            case 0: difficulty.text = "Easy";
                break;
            case 1: difficulty.text = "Moderate";;
                break;
            case 2: difficulty.text = "Hard";;
                break;
        }
        score1Gui.text = 0.ToString();
        score2Gui.text = 0.ToString();
    }
    
    //create initial board state
    void Initialize()
    {
        CreateBoard();
        StartCoroutine(InitializePieces());
    }
    
    //initialize board
    void CreateBoard()
    {
        Slots[] slotsTmp = board.GetComponentsInChildren<Slots>();
        boardSlots = new List<Slots>();

        foreach (Slots slot in slotsTmp)
        {
            slot.Seed = 0;
            boardSlots.Add(slot);
        }
        //Sort the slot position
        boardSlots.OrderBy((slots1 => slots1.slotNum));
    }
    
    //initialize game pieces
    IEnumerator InitializePieces()
    {
        for (int i = 0; i < 14; i++)
        {
            Slots slot = boardSlots[i];
            if (!slot.isStore)
            {
                for (int s = 0; s < 4; s++)
                {
                    CreatePiece(slot.slotNum);
                    yield return new WaitForSeconds(0.001f);
                }
                slot.Seed = 4;
            }
        }
    }
    
    //creates piece game objects
    public void CreatePiece(int slotNumber)
    {
        GameObject stoneGo = GameObject.Instantiate(piecesPrefab, Vector3.zero, Quaternion.identity) as GameObject;
        Transform slotTransform = boardSlots[slotNumber].transform;
        PlacePiece(slotTransform, stoneGo);
    }

    //game logic for player
    void PlayGame(int targetIndex)
    {
        if (CanChooseSlot(targetIndex))
        {
            StartCoroutine(MovePieces(targetIndex));
            CheckWinCon();
        }
        else
        {
            Debug.Log("Invalid slot");
        }
    }
    
    //checks if player can select slot
    public bool CanChooseSlot(int slotNumber)
    {
        if (boardSlots[slotNumber].Seed == 0)
        {
            return false;
        }
        if(boardSlots[slotNumber].Seed > 0 && isPlayer1Turn && boardSlots[slotNumber].player1Slot)
        {
            return true;
        }
        return false;
    }
    
    //gets and returns the value of the next slot on the board
    public int NextSlot(int slotIndex)
    {
        if (slotIndex == 13)
        {
            return 0;
        }
        return slotIndex + 1;
    }
    
    //checks for win condition and updates TMP
    void CheckWinCon()
    {
        bool player1Slots = true;
        bool player2Slots = true;
        int player1Points = 0;
        int player2Points = 0;
        
        // Player 1 Slots
        for (int i = 1; i < 7; i++)
        {
            if (boardSlots[i].Seed > 0)
            {
                player1Slots = false;
                break;
            }
        }

        // Player 2 Slots
        for (int i = 8; i < 14; i++)
        {
            if (boardSlots[i].Seed > 0)
            {
                player2Slots = false;
                break;
            }
        }
        //get and set player points
        player1Points = boardSlots[7].Seed;
        player2Points = boardSlots[0].Seed;
        score1Gui.text = player1Points.ToString();
        score2Gui.text = player2Points.ToString();
        
        //update score
        
        //check if either side is empty
        if (player1Slots || player2Slots)
        {
            //check points to see who wins or if tie
            if (player1Points > player2Points)
            {
                //Player 1 wins
                Debug.Log("Player 1 Wins");
                gameOver.text = "Player 1 Wins";
                panel.SetActive(true);
                player1Won = true;
                AudioSource.PlayClipAtPoint(winGame, transform.position, 1f);
                
            }
            else if (player1Points == player2Points)
            {
                //tie
                Debug.Log("Tie Game");
                gameOver.text = "It's a tie!";
                panel.SetActive(true);
            }
            else
            {
                //player 2 wins
                Debug.Log("Player 2 Wins");
                gameOver.text = "Player 2 Wins";
                panel.SetActive(true);
                player1Won = false;
                AudioSource.PlayClipAtPoint(loseGame, transform.position, 1f);
            }
        }
        

    }
    
    //moves pieces in selected slot
    IEnumerator MovePieces(int slotIndex)
    {
        Slots slot = boardSlots[slotIndex];
        int currentNumPieces = boardSlots[slotIndex].Seed;
        int nextIndex = slotIndex;
        boardSlots[slotIndex].Seed = 0;
        Seeds[] pieces = slot.transform.GetComponentsInChildren<Seeds>();
        Slots nextSlot = boardSlots[nextIndex];
        //need to yeet pieces off map or something
        foreach (Seeds seed in pieces)
        {
            seed.transform.position = Vector3.one * 150;

        }
        while(currentNumPieces > 0)
        {
            GameObject aSeed = pieces[currentNumPieces - 1].gameObject;
            nextIndex = NextSlot(nextIndex);
            //check and skip other players store
            if (isPlayer1Turn && nextIndex == 0)
            {
                nextIndex = NextSlot(nextIndex);
                nextSlot = boardSlots[nextIndex];
                PlacePiece(nextSlot.transform, aSeed);
                boardSlots[nextIndex].Seed++;
            }
            else if (!isPlayer1Turn && nextIndex == 7)
            {
                nextIndex = NextSlot(nextIndex);
                PlacePiece(boardSlots[nextIndex].transform, aSeed);
                boardSlots[nextIndex].Seed++;
            }
            else
            {
                nextSlot = boardSlots[nextIndex];
                PlacePiece(nextSlot.transform, aSeed);
                boardSlots[nextIndex].Seed++;
            }

            currentNumPieces--;
        }
        yield return new WaitForSeconds(stonesSpeed);
    }
    
    private void PlacePiece(Transform slotTransform, GameObject piece)
    {
        piece.transform.parent = slotTransform;  

        
        SphereCollider sc = slotTransform.GetComponent<Collider>() as SphereCollider;
        float posX = UnityEngine.Random.Range(-sc.radius / 2, sc.radius / 2);
        float posY = sc.radius / 2;
        float posZ = UnityEngine.Random.Range(-sc.radius / 2, sc.radius / 2);

        piece.transform.localPosition = new Vector3(posX, posY, posZ);
        piece.transform.rotation = UnityEngine.Random.rotation;

        // drops stones and plays audio
        piece.GetComponent<Rigidbody>().velocity = Vector3.down;
        AudioSource.PlayClipAtPoint(piecesMoving, transform.position, .5f);
    }
    
    //minimax alg with alpha-beta pruning
    int MiniMax(in List<SlotCopy> theBoard, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        int maxEval = -1000000000;
        int minEval = 1000000000;
        List<PosMove> newBoard = new List<PosMove>(); 
        if (depth == 0)
        {
            return StaticEvaluator(theBoard);
        }
        //if maximizing move
        if (maximizingPlayer)
        {
            
            foreach (var move in GetPossibleMoves(theBoard, true)) //gets possible moves
            {
                newBoard.Add(MakeAIMove(move, theBoard, true)); //adds corresponding game board
            }

          
            foreach (var possibleBoard in newBoard)
            {
                int eval = MiniMax(possibleBoard.tempCopy, depth - 1, alpha, beta, false);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            
            return maxEval;
        }
        //if minimizing move
        else
        {
            
            foreach (var move in GetPossibleMoves(theBoard, false)) //gets possible moves
            {
                newBoard.Add(MakeAIMove(move, theBoard, false)); //adds corresponding game board
            }

           
            foreach (var possibleBoard in newBoard)
            {
                int eval = MiniMax(possibleBoard.tempCopy, depth - 1, alpha, beta, true);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            
            return minEval;
        }
    }

    //evaluates condition of the board
    int StaticEvaluator(in List<SlotCopy> slot)
    {
        int player1Points = slot[7].Seeds; 
        int player2Points = slot[0].Seeds; 
        return player2Points - player1Points;
    }
    
    //game logic for AI move
    int AI(in List<SlotCopy> temp)
    {
        int depth = 0;
        switch (Difficulty.Value)
        {
            case 0: depth = 2;
                break;
            case 1: depth = 4;
                break;
            case 2: depth = 6;
                break;
        }
        List<PosMove> gameStates = new List<PosMove>(); //possible board states based on moves
        int alpha = -100000;
        int beta = 100000;
        int best_score = StaticEvaluator(temp);
        int best_move = 0;
        //get possible movements for AI from current board
        foreach (var move in GetPossibleMoves(temp, true)) //gets possible moves
        {
            Debug.Log("Look at moves");
            Debug.Log(move);
            gameStates.Add(MakeAIMove(move, temp, true)); //adds corresponding game board
        }
        //runs minimax on possible board states
        foreach (var possibleBoard in gameStates)
        {
            int score = MiniMax(possibleBoard.tempCopy, depth, alpha, beta, false);
            if (score > best_score)
            {
                best_move = possibleBoard.moveNum;
                best_score = score;
            }

            else if (best_move == 0)
            {
                best_move = possibleBoard.moveNum;
            }
        }
        return best_move;
    }
    
    //makes move on a temp board to evaluate the state
    PosMove MakeAIMove(int slotChoice, in List<SlotCopy> currentBoard, bool isMax)
    {
        List<SlotCopy> newBoardState = new List<SlotCopy>();
        foreach (var s in currentBoard)
        {
            SlotCopy newSlot = new SlotCopy();
            newSlot.player1 = s.player1;
            newSlot.slotNumber = s.slotNumber;
            newSlot.isaStore = s.isaStore;
            newSlot.Seeds = s.Seeds;
            newBoardState.Add(newSlot);
        }
        //newBoardState.OrderBy((slots1 => slots1.slotNumber));
        
        int currentNumPieces = currentBoard[slotChoice].Seeds;
        int nextIndex = slotChoice;
        newBoardState[slotChoice].Seeds = 0;
        
        while(currentNumPieces > 0)
        {
            nextIndex = NextSlot(nextIndex);
            //check and skip other players store
            if (!isMax && nextIndex == 0)
            {
                nextIndex = NextSlot(nextIndex);
                newBoardState[nextIndex].Seeds++;
            }
            else if (isMax && nextIndex == 7)
            {
                nextIndex = NextSlot(nextIndex);
                newBoardState[nextIndex].Seeds++;
            }
            else
            {
                newBoardState[nextIndex].Seeds++;
            }
            currentNumPieces--;
        }

        PosMove temp = new PosMove();
        temp.moveNum = slotChoice;
        foreach (var VARIABLE in newBoardState)
        {
            SlotCopy temp1 = new SlotCopy();
            temp1 = VARIABLE;
                
            temp.tempCopy.Add(temp1);
        }
        temp.tempCopy.OrderBy((slots1 => slots1.slotNumber));
        return temp;
    }
    
    //gets the possible moves for specified player
    List<int> GetPossibleMoves(in List<SlotCopy> gameboard, bool isMax)
    {
        List<int> possibleMovements = new List<int>();
        if (!isMax)
        {
            for (int i = 1; i < 7; i++)
            {
                if (gameboard[i].Seeds > 0)
                {
                    possibleMovements.Add(i);
                }
            }
        }
        else
        {
            for (int i = 8; i < 14; i++)
            {
                if (gameboard[i].Seeds > 0)
                {
                    possibleMovements.Add(i);
                }
            }
        }

        return possibleMovements;
    }

}
