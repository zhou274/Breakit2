using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;
using TMPro;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;
using StarkSDKSpace;
using UnityEngine.Analytics;

public class GameManager : MonoBehaviour {

	//visible in the inspector
	public TextMeshProUGUI[] scoreTexts;
	public Animator scoreAnim;
	public Animator fade;
	public Animator startPanel;
	public Animator gamePanel;
	public Animator shopLabel;
	public GameObject shopPanel;
	public GameObject tutorialPanel;
	public Animator camZoom;
	public CameraMovement cam;

	public Image progressBar;

	public AudioSource lose;
	public AudioSource win;

	[HideInInspector]
	public bool gameOver;

	int score;
	bool loading;

	bool started;

	Transform finish;
	Transform player;

	float startDistance;

	public GameObject GameFailPanel;

	public static bool isPause;

    public string clickid;
    private StarkAdManager starkAdManager;
    void Start() {
		//deactivate the shop and camera script
		shopPanel.SetActive(false);
		cam.enabled = false;

		//get the player
		player = GameObject.FindObjectOfType<Player>().transform;
	}

	void Update() {
		if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) {
			if (!(Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))) {
				//start the game when we first drag finger on the screen
				if (!started && !shopPanel.activeSelf)
					StartGame();
			}

			//if it's not yet loading, but we want to continue, load the next level
			if (gameOver && !loading)
				NextLevel();
		}
		//if(isPause==true)
		//{
		//	Time.timeScale = 0;
		//	if (Input.GetMouseButtonDown(0))
		//	{
		//		isPause = false;
		//		Time.timeScale = 1;
		//	}
		//}
		//show progress by getting the distance between the player and the finish line
		if (finish != null && player != null) {
			float distance = Vector3.Distance(finish.position, player.position);

			float percentage = (startDistance - distance) / startDistance;
			progressBar.fillAmount = percentage;
		}
	}

	//start the game by showing game UI and enabling the camera script
	void StartGame() {
		startPanel.SetTrigger("Fade out");
		gamePanel.SetBool("Show", true);

		cam.enabled = true;
		camZoom.enabled = false;

		started = true;
	}

	//reload scene for the next level
	void NextLevel() {
		StartCoroutine(Load(0));

		loading = true;
	}

	//play lose sound and reload scene to restart this level
	public void ReloadScene(float delay) {
		TryAd();
		lose.Play();
		StartCoroutine(Load(delay));
	}


	public void PauseScene(float delay)
	{
		lose.Play();
        StartCoroutine(Pause(delay));
        
    }
	public void Resume()
	{
        ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {

                    GameFailPanel.SetActive(false);
                    isPause = true;
                    Time.timeScale = 1;
                    Player.Respawned();


                    clickid = "";
                    getClickid();
                    apiSend("game_addiction", clickid);
                    apiSend("lt_roi", clickid);


                }
                else
                {
                    StarkSDKSpace.AndroidUIManager.ShowToast("观看完整视频才能获取奖励哦！");
                }
            },
            (it, str) => {
                Debug.LogError("Error->" + str);
                //AndroidUIManager.ShowToast("广告加载异常，请重新看广告！");
            });
        
    }
    IEnumerator Pause(float delay)
	{
        yield return new WaitForSeconds(delay);

        Time.timeScale = 0;
        GameFailPanel.SetActive(true);
        ShowInterstitialAd("1lcaf5895d5l1293dc",
            () => {
                Debug.LogError("--插屏广告完成--");

            },
            (it, str) => {
                Debug.LogError("Error->" + str);
            });
    }
    //wait for a while, show the black fade effect and load the current scene
    IEnumerator Load(float delay){
		yield return new WaitForSeconds(delay);
		
		fade.SetTrigger("Fade");
		
		yield return new WaitForSeconds(0.4f);
		
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
	
	//add some points to the score
	public void AddPoints(int points){
		score += points;
		
		StartCoroutine(UpdateScore());
	}
	
	//return score
	public int GetScore(){
		return score;
	}
	
	//update the score text and show a small effect
	IEnumerator UpdateScore(){
		scoreAnim.SetTrigger("Effect");
		
		yield return new WaitForSeconds(1f/6f);
		
		for(int i = 0; i < scoreTexts.Length; i++){
			scoreTexts[i].text = score == 0 ? "" : "" + score;
		}
	}
	
	//open or close the shop depending on its current state
	public void Shop(){
		shopPanel.SetActive(!shopPanel.activeSelf);
		tutorialPanel.SetActive(!tutorialPanel.activeSelf);
		
		camZoom.SetBool("Zoom", !camZoom.GetBool("Zoom"));
		
		shopLabel.SetBool("Show", !shopLabel.GetBool("Show"));
	}
	
	//set the finish line transform and get the starting distance between the player and the finish line
	public void SetFinishLine(Transform finish){
		this.finish = finish;
		
		if(finish != null && player != null)
			startDistance = Vector3.Distance(finish.position, player.position);
	}
	
	void TryAd(){
		#if UNITY_ADS
		AdManager adManager = GameObject.FindObjectOfType<AdManager>();
		
		if(adManager == null)
			return;
		
		adManager.Interstitial();
		#endif
	}



    public void getClickid()
    {
        var launchOpt = StarkSDK.API.GetLaunchOptionsSync();
        if (launchOpt.Query != null)
        {
            foreach (KeyValuePair<string, string> kv in launchOpt.Query)
                if (kv.Value != null)
                {
                    Debug.Log(kv.Key + "<-参数-> " + kv.Value);
                    if (kv.Key.ToString() == "clickid")
                    {
                        clickid = kv.Value.ToString();
                    }
                }
                else
                {
                    Debug.Log(kv.Key + "<-参数-> " + "null ");
                }
        }
    }

    public void apiSend(string eventname, string clickid)
    {
        TTRequest.InnerOptions options = new TTRequest.InnerOptions();
        options.Header["content-type"] = "application/json";
        options.Method = "POST";

        JsonData data1 = new JsonData();

        data1["event_type"] = eventname;
        data1["context"] = new JsonData();
        data1["context"]["ad"] = new JsonData();
        data1["context"]["ad"]["callback"] = clickid;

        Debug.Log("<-data1-> " + data1.ToJson());

        options.Data = data1.ToJson();

        TT.Request("https://analytics.oceanengine.com/api/v2/conversion", options,
           response => { Debug.Log(response); },
           response => { Debug.Log(response); });
    }


    /// <summary>
    /// </summary>
    /// <param name="adId"></param>
    /// <param name="closeCallBack"></param>
    /// <param name="errorCallBack"></param>
    public void ShowVideoAd(string adId, System.Action<bool> closeCallBack, System.Action<int, string> errorCallBack)
    {
        starkAdManager = StarkSDK.API.GetStarkAdManager();
        if (starkAdManager != null)
        {
            starkAdManager.ShowVideoAdWithId(adId, closeCallBack, errorCallBack);
        }
    }

    /// <summary>
    /// 播放插屏广告
    /// </summary>
    /// <param name="adId"></param>
    /// <param name="errorCallBack"></param>
    /// <param name="closeCallBack"></param>
    public void ShowInterstitialAd(string adId, System.Action closeCallBack, System.Action<int, string> errorCallBack)
    {
        starkAdManager = StarkSDK.API.GetStarkAdManager();
        if (starkAdManager != null)
        {
            var mInterstitialAd = starkAdManager.CreateInterstitialAd(adId, errorCallBack, closeCallBack);
            mInterstitialAd.Load();
            mInterstitialAd.Show();
        }
    }
}
