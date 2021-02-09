// MIT License
//
// Copyright (c) 2021 Filipe Manana <fdmanana@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;

public class GameManager : MonoBehaviour
{
    private enum State {
        Playing,
        Paused,
        GameOver,
    };

    public static GameManager gm;

    public GameObject[] tetrisPiecePrefabs;

    // The timeout after which a piece is moved down 1 unit automatically.
    public float startTimeout = 2.0f;
    public float minTimeout = 0.3f;
    private float timeout;
    private float playingTime = 0;

    private State state = State.Playing;

    // Setup in the editor.
    public GameObject gameCanvas;
    public GameObject pauseCanvas;
    public GameObject gameOverCanvas;
    public GameObject mainCamera;
    public GameObject nextPieceText;
    public AudioClip gameOverAudioClip;

    private int score = 0;

    private int nextPieceIndex = -1;
    private GameObject nextPiece = null;

    private Text scoreText;
    private Text highScoreText;

    // Start is called before the first frame update
    void Awake()
    {
        if (gm == null) {
            gm = this.gameObject.GetComponent<GameManager>();
        }
        gameCanvas.SetActive(true);
        gameOverCanvas.SetActive(false);
        pauseCanvas.SetActive(false);
    }

    void Start()
    {
        timeout = startTimeout;
        StartGame();
    }

    void Update()
    {
        if (state == State.Playing &&
            (Input.GetKeyDown(GameSettings.helpKey) || Input.GetKeyDown(GameSettings.pauseKey))) {
            PauseGame();
        } else if (state == State.Paused &&
            (Input.GetKeyDown(GameSettings.helpKey) || Input.GetKeyDown(GameSettings.pauseKey))) {
            UnpauseGame();
        }

        playingTime += Time.deltaTime;
        if (playingTime >= GameSettings.timeTimeoutTrigger) {
            UpdateTimeout();
            playingTime = 0;
        }
    }

    private void UpdateTimeout()
    {
        float newTimeout = Mathf.Max(timeout - 0.1f, minTimeout);

        if (newTimeout != timeout) {
            Debug.Log("Timeout updated from " + timeout + " to " + newTimeout);
        }
        timeout = newTimeout;
    }

    public float GetTimeout()
    {
        return timeout;
    }

    private void PauseGame()
    {
        Assert.IsTrue(state == State.Playing);
        state = State.Paused;
        gameCanvas.SetActive(false);
        pauseCanvas.SetActive(true);
        UpdateScoreObjects();
        UpdateScoreText();
    }

    public void UnpauseGame()
    {
        Assert.IsTrue(state == State.Paused);
        gameCanvas.SetActive(true);
        pauseCanvas.SetActive(false);
        state = State.Playing;
        UpdateScoreObjects();
        UpdateScoreText();
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene("TetrisLevel");
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void ResetHighScore()
    {
        PlayerPrefs.SetInt("HighScore", 0);
        UpdateScoreText();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GameOver()
    {
        nextPieceIndex = -1;
        if (nextPiece != null) {
            Destroy(nextPiece);
            nextPiece = null;
        }
        state = State.GameOver;
        gameCanvas.SetActive(false);
        gameOverCanvas.SetActive(true);
        UpdateScoreObjects();
        UpdateScoreText();
        AudioSource.PlayClipAtPoint(gameOverAudioClip, transform.position);
    }

    public bool IsGameActive()
    {
        return state == State.Playing;
    }

    public void StartGame()
    {
        state = State.Playing;
        UpdateScoreObjects();
        SetScore(0);
        SpawnNextPiece();
    }

    public void SpawnNextPiece()
    {
        Vector3 nextPiecePos;
        int newNextIndex = Random.Range(0, tetrisPiecePrefabs.Length);

        if (nextPieceIndex == -1) {
            nextPieceIndex = Random.Range(0, tetrisPiecePrefabs.Length);
        }

        if (nextPiece != null) {
            nextPiecePos = nextPiece.transform.position;
            Destroy(nextPiece);
        } else {
            nextPiecePos = nextPieceText.transform.position;
        }

        nextPiece = Instantiate(tetrisPiecePrefabs[newNextIndex],
                                transform.position,
                                transform.rotation) as GameObject;
        TetrisPieceController pieceController = nextPiece.GetComponent<TetrisPieceController>();
        pieceController.SetUnplayable();

        nextPiece.transform.position = nextPiecePos;
        nextPiece.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        nextPiece.transform.SetParent(mainCamera.transform);

        GameObject piece = Instantiate(tetrisPiecePrefabs[nextPieceIndex],
                                       transform.position,
                                       transform.rotation) as GameObject;

        nextPieceIndex = newNextIndex;
    }

    public void IncrementScore(int amount)
    {
        SetScore(this.score + amount);
    }

    private void SetScore(int score)
    {
        int highScore = PlayerPrefs.GetInt("HighScore", 0);

        this.score = score;
        if (score > highScore) {
            PlayerPrefs.SetInt("HighScore", score);
        }
        UpdateScoreText();
        if (score > 0 && ((score % GameSettings.scoreTimeoutTrigger) == 0)) {
            UpdateTimeout();
        }
    }

    private void UpdateScoreText()
    {
        int highScore = PlayerPrefs.GetInt("HighScore", 0);

        scoreText.text = "Score: " + this.score.ToString();
        highScoreText.text = "High Score: " + highScore.ToString();
    }

    private void UpdateScoreObjects()
    {
        GameObject canvas = null;
        Transform t;

        switch (state) {
        case State.Playing:
            canvas = gameCanvas;
            break;
        case State.GameOver:
            canvas = gameOverCanvas;
            break;
        case State.Paused:
            canvas = pauseCanvas;
            break;
        }

        t = canvas.transform.Find("ScoreText");
        scoreText = t.gameObject.GetComponent<Text>();
        t = canvas.transform.Find("HighScoreText");
        highScoreText = t.gameObject.GetComponent<Text>();
    }
}
