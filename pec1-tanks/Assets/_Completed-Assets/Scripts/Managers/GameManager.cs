using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Complete
{
    public class GameManager : MonoBehaviour
    {
        [Header("PEC added variables")]
        public GameObject startManager;
        private StartManager starting_players;
        private int players_on_start;
        private int player_waiting = 0;
        private Camera[] allcameras = { null, null, null, null };
        public Camera[] deathCameras;

        public float gameEnd_delay = 3f;
        private WaitForSeconds gameEnd_wait;

        public GameObject mapView;
        private Camera mapCam;

        [Header("2 players UI variables")]
        public GameObject UI_2players;
        public Text[] wins_2P;
        public GameObject add_texts;
        public Text waitingPlayers_text;

        [Header("Multiple players UI variables")]
        public GameObject UI_MultPlayers;
        public Text[] wins_MP;
        public Text fourthPlayerWaiting;

        [Header("Main Gameplay variables")]
        public int m_NumRoundsToWin = 5;            // The number of rounds a single player has to win to win the game
        public float m_StartDelay = 3f;             // The delay between the start of RoundStarting and RoundPlaying phases
        public float m_EndDelay = 3f;               // The delay between the end of RoundPlaying and RoundEnding phases
        public CameraControl m_CameraControl;       // Reference to the CameraControl script for control during different phases
        public Text m_MessageText;                  // Reference to the overlay Text to display winning text, etc.
        public GameObject m_TankPrefab;             // Reference to the prefab the players will control
        public TankManager[] m_Tanks;               // A collection of managers for enabling and disabling different aspects of the tanks
        
        private int m_RoundNumber;                  // Which round the game is currently on
        private WaitForSeconds m_StartWait;         // Used to have a delay whilst the round starts
        private WaitForSeconds m_EndWait;           // Used to have a delay whilst the round or game ends
        private TankManager m_RoundWinner;          // Reference to the winner of the current round.  Used to make an announcement of who won
        private TankManager m_GameWinner;           // Reference to the winner of the game.  Used to make an announcement of who won


        private void Start()
        {
            // Create the delays so they only have to be made once
            m_StartWait = new WaitForSeconds (m_StartDelay);
            m_EndWait = new WaitForSeconds (m_EndDelay);
            gameEnd_wait = new WaitForSeconds(gameEnd_delay);

            starting_players = startManager.GetComponent<StartManager>();
            players_on_start = starting_players.GetStartPlayers();

            UI_2players.SetActive(false);
            UI_MultPlayers.SetActive(false);

            for (int i = 0; i < deathCameras.Length; i++)
            {
                deathCameras[i].gameObject.SetActive(false);
            }

            mapView.SetActive(false);
            mapCam = mapView.gameObject.GetComponent<Camera>();
            mapCam.rect = new Rect(0.5f, 0.0f, 0.5f, 0.5f);
            SpawnAllTanks();
            SetCameraTargets();

            // Once the tanks have been created and the camera is using them as targets, start the game
            StartCoroutine (GameLoop());
        }

        private void Update()
        {
            if (players_on_start < 4)
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    player_waiting++;
                    if (players_on_start == 2)
                    {
                        if (player_waiting >= 2)
                        {
                            player_waiting = 2;
                        }
                        waitingPlayers_text.text = "Players waiting:\n" + player_waiting;
                    }
                    if (players_on_start == 3)
                    {
                        fourthPlayerWaiting.text = "New player waiting";
                    }
                }
            }
            else
            {
                fourthPlayerWaiting.text = "";
            }

            for (int i = 0; i < players_on_start; i++)
            {
                if (m_Tanks[i].m_Instance.GetComponent<TankHealth>().GetDead())
                {
                    deathCameras[i].gameObject.SetActive(true);
                }
            }
        }

        private void SpawnAllTanks()
		{
			Camera mainCam = GameObject.Find ("Main Camera").GetComponent<Camera>();

			// For all the tanks...
			for (int i = 0; i < m_Tanks.Length; i++)
			{
				// ... create them, set their player number and references needed for control
				m_Tanks[i].m_Instance = Instantiate (m_TankPrefab, m_Tanks[i].m_SpawnPoint.position, m_Tanks[i].m_SpawnPoint.rotation) as GameObject;
				m_Tanks[i].m_PlayerNumber = i + 1;
				m_Tanks[i].Setup();
                AddCamera(i, mainCam);
                if (i > players_on_start - 1)
                {
                    m_Tanks[i].m_Instance.SetActive(false);
                }
			}
			mainCam.gameObject.SetActive (false);
		}

		private void AddCamera (int i, Camera mainCam)
        {
			GameObject childCam = new GameObject ("Camera" + (i + 1));
			Camera newCam = childCam.AddComponent<Camera>();		
			newCam.CopyFrom (mainCam);
			childCam.transform.parent = m_Tanks[i].m_Instance.transform;

            if (players_on_start > 2)
            {
                switch (i)
                {
                    case 0:
                        newCam.rect = new Rect(0.0f, 0.5f, 0.5f, 0.5f);
                        break;
                    case 1:
                        newCam.rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                        break;
                    case 2:
                        newCam.rect = new Rect(0.0f, 0.0f, 0.5f, 0.5f);
                        break;
                    case 3:
                        newCam.rect = new Rect(0.5f, 0.0f, 0.5f, 0.5f);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (i == 0)
                {
                    newCam.rect = new Rect(0.0f, 0.5f, 0.89f, 0.5f);
                }
                else
                {
                    newCam.rect = new Rect(0.11f, 0.0f, 0.89f, 0.5f);
                }
            }
            allcameras[i] = newCam;
		}


        private void SetCameraTargets()
        {
            // Create a collection of transforms the same size as the number of tanks
            Transform[] targets = new Transform[m_Tanks.Length];

            // For each of these transforms...
            for (int i = 0; i < players_on_start; i++)
            {
                // ... set it to the appropriate tank transform
                targets[i] = m_Tanks[i].m_Instance.transform;
            }

            // These are the targets the camera should follow
            m_CameraControl.m_Targets = targets;
        }


        // This is called from start and will run each phase of the game one after another
        private IEnumerator GameLoop()
        {
            // Start off by running the 'RoundStarting' coroutine but don't return until it's finished
            yield return StartCoroutine (RoundStarting());

            // Once the 'RoundStarting' coroutine is finished, run the 'RoundPlaying' coroutine but don't return until it's finished
            yield return StartCoroutine (RoundPlaying());

            // Once execution has returned here, run the 'RoundEnding' coroutine, again don't return until it's finished
            yield return StartCoroutine (RoundEnding());

            // This code is not run until 'RoundEnding' has finished.  At which point, check if a game winner has been found
            if (m_GameWinner == null)
            {
                StartCoroutine (GameLoop());
            }
        }


        private IEnumerator RoundStarting()
        {
            for (int i = 0; i < deathCameras.Length; i++)
            {
                deathCameras[i].gameObject.SetActive(false);
            }
            if (players_on_start > 2)
            {
                UI_2players.SetActive(false);
                UI_MultPlayers.SetActive(true);
                
                allcameras[0].rect = new Rect(0.0f, 0.5f, 0.5f, 0.5f);
                deathCameras[0].rect = new Rect(0.0f, 0.5f, 0.5f, 0.5f);
                
                allcameras[1].rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                deathCameras[1].rect = new Rect(0.5f, 0.5f, 0.5f, 0.5f);

                allcameras[2].rect = new Rect(0.0f, 0.0f, 0.5f, 0.5f);
                deathCameras[2].rect = new Rect(0.0f, 0.0f, 0.5f, 0.5f);

                allcameras[3].rect = new Rect(0.5f, 0.0f, 0.5f, 0.5f);
                deathCameras[3].rect = new Rect(0.5f, 0.0f, 0.5f, 0.5f);
            }
            else
            {
                UI_MultPlayers.SetActive(false);
                UI_2players.SetActive(true);
                for (int i = 0; i < wins_2P.Length; i++)
                {
                    wins_2P[i].text = "Tank " + (i + 1) + " wins: " + m_Tanks[i].m_Wins;
                }
            }
            if (players_on_start == 3)
            {
                mapView.SetActive(true);
                for (int i = 0; i < wins_MP.Length; i++)
                {
                    wins_MP[i].text = "Tank " + (i + 1) + " wins: " + m_Tanks[i].m_Wins;
                    if (i == 4)
                    {
                        wins_MP[i].text = "";
                    }
                }
            }
            else
            {
                mapView.SetActive(false);
                for (int i = 0; i < wins_MP.Length; i++)
                {
                    wins_MP[i].text = "Tank " + (i + 1) + " wins: " + m_Tanks[i].m_Wins;
                }
            }

            // As soon as the round starts reset the tanks and make sure they can't move
            ResetAllTanks();
            DisableTankControl();
            Debug.Log("Number of players now: " + players_on_start);
            // Snap the camera's zoom and position to something appropriate for the reset tanks
            m_CameraControl.SetStartPositionAndSize();

            // Increment the round number and display text showing the players what round it is
            m_RoundNumber++;
            m_MessageText.text = "ROUND " + m_RoundNumber;

            // Wait for the specified length of time until yielding control back to the game loop
            yield return m_StartWait;
        }


        private IEnumerator RoundPlaying()
        {
            // As soon as the round begins playing let the players control the tanks
            EnableTankControl();

            // Clear the text from the screen
            m_MessageText.text = string.Empty;

            // While there is not one tank left...
            while (!OneTankLeft())
            {
                // ... return on the next frame
                yield return null;
            }

        }


        private IEnumerator RoundEnding()
        {
            // Stop tanks from moving
            DisableTankControl();

            if (player_waiting > 0)
            {
                players_on_start += player_waiting;
                if (players_on_start >= 4)
                {
                    players_on_start = 4;
                }

            }

            

            // Clear the winner from the previous round
            m_RoundWinner = null;

            // See if there is a winner now the round is over
            m_RoundWinner = GetRoundWinner();

            // If there is a winner, increment their score
            if (m_RoundWinner != null)
                m_RoundWinner.m_Wins++;

            // Now the winner's score has been incremented, see if someone has one the game
            m_GameWinner = GetGameWinner();
            
            // Get a message based on the scores and whether or not there is a game winner and display it
            string message = EndMessage();
            m_MessageText.text = message;

            if (m_GameWinner != null)
            {
                yield return gameEnd_wait;
                SceneManager.LoadScene("Main-menu");
            }
            else
            {
                // Wait for the specified length of time until yielding control back to the game loop
                yield return m_EndWait;
            }
            
        }


        // This is used to check if there is one or fewer tanks remaining and thus the round should end
        private bool OneTankLeft()
        {
            // Start the count of tanks left at zero
            int numTanksLeft = 0;

            // Go through all the tanks...
            for (int i = 0; i < players_on_start; i++)
            {
                // ... and if they are active, increment the counter
                if (m_Tanks[i].m_Instance.activeSelf)
                    numTanksLeft++;
            }

            // If there are one or fewer tanks remaining return true, otherwise return false
            return numTanksLeft <= 1;
        }
        
        
        // This function is to find out if there is a winner of the round
        // This function is called with the assumption that 1 or fewer tanks are currently active
        private TankManager GetRoundWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < players_on_start; i++)
            {
                // ... and if one of them is active, it is the winner so return it
                if (m_Tanks[i].m_Instance.activeSelf)
                {
                    return m_Tanks[i];
                }
            }

            // If none of the tanks are active it is a draw so return null
            return null;
        }


        // This function is to find out if there is a winner of the game
        private TankManager GetGameWinner()
        {
            // Go through all the tanks...
            for (int i = 0; i < players_on_start; i++)
            {
                // ... and if one of them has enough rounds to win the game, return it
                if (m_Tanks[i].m_Wins == m_NumRoundsToWin)
                {
                    return m_Tanks[i];
                }
            }

            // If no tanks have enough rounds to win, return null
            return null;
        }


        // Returns a string message to display at the end of each round.
        private string EndMessage()
        {
            // By default when a round ends there are no winners so the default end message is a draw
            string message = "DRAW!";

            // If there is a winner then change the message to reflect that
            if (m_RoundWinner != null)
            {
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";
            }

            // Add some line breaks after the initial message
            message += "\n\n\n\n";

            // Go through all the tanks and add each of their scores to the message
            for (int i = 0; i < players_on_start; i++)
            {
                message += m_Tanks[i].m_ColoredPlayerText + ": " + m_Tanks[i].m_Wins + " WINS\n";
            }

            // If there is a game winner, change the entire message to reflect that
            if (m_GameWinner != null)
            {
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";
            }

            return message;
        }


        // This function is used to turn all the tanks back on and reset their positions and properties
        private void ResetAllTanks()
        {
            for (int i = 0; i < players_on_start; i++)
            {
                m_Tanks[i].Reset();
            }
        }


        private void EnableTankControl()
        {
            for (int i = 0; i < players_on_start; i++)
            {
                m_Tanks[i].EnableControl();
            }
        }


        private void DisableTankControl()
        {
            for (int i = 0; i < players_on_start; i++)
            {
                m_Tanks[i].DisableControl();
            }
        }

        public int GetCurrentPlayers()
        {
            return players_on_start;
        }
    }
}