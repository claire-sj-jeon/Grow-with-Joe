using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;



//https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2FbabyjoeVideo.mp4?alt=media&token=8195f766-4c49-4d17-8a22-742af639fd8d
//https://firebasestorage.googleapis.com/v0/b/bohosoo.appspot.com/o/movies%2Fjoeplay.mp4?alt=media&token=67c9e8aa-0e33-4ba3-b99c-0b7be6f0942e
[RequireComponent(typeof(VideoPlayer))]
public class VideoRawImagePlayer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage targetRawImage;
    [SerializeField] private AspectRatioFitter aspectRatioFitter;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] UIEffect effectRoot;
    [SerializeField] UIEffect effectImage;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private string url;    
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = false;
    [SerializeField] private bool mute = false;
    
    
    private RenderTexture renderTexture;
    private bool isFinished = false;

    // Prepare 가 끝나도 렌더텍스처에 첫 프레임이 그려지기 전까지는 빈 화면이 보인다.
    // 그 사이를 가리기 위해 실제 프레임이 들어올 때까지 기다린다.
    private bool waitingFirstFrame = false;
    public bool IsFinished => isFinished;
    public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;



    private void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        SetupVideoPlayer();
        Activity = false;
        SetRawImageAlpha(0.0f);
    }

    private void Start()
    {
        if (videoClip != null && playOnStart)
        {
            Play(videoClip);
        }


        if(url != null && playOnStart)
        {
            PlayUrl(url);
        }
    }

    private void Update()
    {
        if (waitingFirstFrame == false)
            return;

        // frame 은 첫 프레임이 출력되기 전까지 음수다.
        if (videoPlayer == null || videoPlayer.frame < 0)
            return;

        waitingFirstFrame = false;
        SetRawImageAlpha(1.0f);
    }

    private void OnDestroy()
    {
        ReleaseRenderTexture();
    }

    // RawImage 는 텍스처가 비어 있으면 흰 사각으로 그려진다.
    // GameObject 를 끄면 effectImage 의 연출과 엉키므로 색 투명도만 건드린다.
    private void SetRawImageAlpha(float alpha)
    {
        if (targetRawImage == null)
            return;

        var color = targetRawImage.color;
        color.a = alpha;
        targetRawImage.color = color;
    }

    public bool Activity
    {
        get
        {
            return effectRoot.Activity;
        }
        set
        {
            effectRoot.Activity = value;
        }
    }

    private void SetupVideoPlayer()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.isLooping = loop;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.SetDirectAudioMute(0, mute);

        videoPlayer.prepareCompleted -= OnPrepareCompleted;
        videoPlayer.prepareCompleted += OnPrepareCompleted;

        videoPlayer.errorReceived -= OnErrorReceived;
        videoPlayer.errorReceived += OnErrorReceived;

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    public void Play(VideoClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("Play 실패: VideoClip이 null 입니다.");
            return;
        }
        isFinished = false;
        videoClip = clip;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = loop;

        PrepareAndPlay();

        
    }

    public void Play()
    {
        if (videoClip == null)
        {
            Debug.LogWarning("재생할 VideoClip이 없습니다.");
            return;
        }

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = loop;

        

        PrepareAndPlay();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        isFinished = true;
        Debug.Log("비디오 재생 완료");
    }

    public void PlayUrl(string url)
    {
        Debug.Log($"PlayUrl==> {url}");

        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning("PlayUrl 실패: url 이 비어있습니다.");
            return;
        }
        isFinished = false;
        videoClip = null;

        videoClip = null;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.isLooping = loop;

        PrepareAndPlay();
    }

    public void Hide()
    {
        effectRoot.Activity = false;
    }

    public void Stop()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();


        effectImage.Activity = false;

        waitingFirstFrame = false;
        SetRawImageAlpha(0.0f);

        //effectRoot.Activity = false;
        //Cinematic shot of[Dynamic action of muscular Black male tennis player in ready stance with racket in red dust], dramatic red-orange color grading, high contrast cinematic lighting with deep shadows and rim highlights, airborne dust smoke and particulate effects, cracked rough earthy textures, intense moody atmosphere, volumetric depth, dynamic framing
    }

    public void Pause()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Pause();
    }

    public void Resume()
    {
        if (videoPlayer != null && !videoPlayer.isPlaying)
            videoPlayer.Play();
    }

    private void PrepareAndPlay()
    {
        if (videoPlayer == null)
            return;

        bool hasClip = videoPlayer.source == VideoSource.VideoClip && videoPlayer.clip != null;
        bool hasUrl = videoPlayer.source == VideoSource.Url && !string.IsNullOrWhiteSpace(videoPlayer.url);

        if (!hasClip && !hasUrl)
            return;

        ReleaseRenderTexture();



        videoPlayer.targetTexture = null;

        if (targetRawImage != null)
            targetRawImage.texture = null;

        // 준비가 끝나 첫 프레임이 나올 때까지 감춰 둔다.
        waitingFirstFrame = false;
        SetRawImageAlpha(0.0f);

        videoPlayer.Prepare();
    }

    private void OnPrepareCompleted(VideoPlayer vp)
    {
        int width = (int)vp.width;
        int height = (int)vp.height;

        if (width <= 0 || height <= 0)
        {
            width = 1920;
            height = 1080;
        }

        renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();

        vp.targetTexture = renderTexture;

        if (targetRawImage != null)
            targetRawImage.texture = renderTexture;

        if (aspectRatioFitter != null)
            aspectRatioFitter.aspectRatio = (float)width / height;

        vp.Play();

        // Play 를 불러도 첫 프레임은 다음 프레임부터 들어온다. Update 에서 드러낸다.
        SetRawImageAlpha(0.0f);
        waitingFirstFrame = true;

        effectImage.Activity = true;
        
        effectImage.Alpha = 0.0f;
        effectImage.AlphaEffect();

    }

    private void OnErrorReceived(VideoPlayer vp, string message)
    {
        Debug.LogError($"비디오 재생 에러: {message}");
    }

    private void ReleaseRenderTexture()
    {
        if (videoPlayer != null && videoPlayer.targetTexture != null)
        {
            videoPlayer.targetTexture = null;
        }

        if (renderTexture != null)
        {
            if (targetRawImage != null && targetRawImage.texture == renderTexture)
                targetRawImage.texture = null;

            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
    }

    public void SetLoop(bool value)
    {
        loop = value;

        if (videoPlayer != null)
            videoPlayer.isLooping = value;
    }

    public void SetMute(bool value)
    {
        mute = value;

        if (videoPlayer != null)
            videoPlayer.SetDirectAudioMute(0, value);
    }
}
