using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace Concept.UI
{

    [UxmlElement]
    public partial class VideoElement : VisualElement
    {
        private const string USSClassName = "twinny-video-element";

        private VideoPlayer m_player;
        private RenderTexture m_renderTexture;
        private VideoClip m_videoClip;

        [UxmlAttribute("media")]
        public VideoClip videoClip
        {
            get => m_videoClip; set
            {
                m_videoClip = value;
                if (value != null) LoadMediaClip(value);

            }
        }

        private string m_videoURL;
        [UxmlAttribute("media-url")]
        public string videoURL
        {
            get => m_videoURL; set
            {
                m_videoURL = value;
                if (!string.IsNullOrEmpty(value)) LoadMediaURL(value);
            }

        }


        [UxmlAttribute("auto-play")]
        public bool autoPlay = true;
        [UxmlAttribute("looping")]
        public bool looping = true;
        [UxmlAttribute("audio-output")]
        public VideoAudioOutputMode audioOutput = VideoAudioOutputMode.Direct;


        public Action OnVideoReady;

        public VideoElement()
        {
            AddToClassList(USSClassName);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            LoadMediaClip(m_videoClip);

        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (m_player != null)
            {
                m_player.prepareCompleted -= OnVideoPrepared;
                m_player.Stop();
                UnityEngine.Object.DestroyImmediate(m_player.gameObject);
                m_player = null;
            }

            if (m_renderTexture != null)
            {
                m_renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(m_renderTexture);
                m_renderTexture = null;
            }
        }

        private void CreateMediaPlayer()
        {
            GameObject go = new GameObject("[VideoPlayer] " + name);
            go.hideFlags = HideFlags.HideAndDontSave;
            m_player = go.AddComponent<VideoPlayer>();
            m_player.source = VideoSource.VideoClip;
            m_player.playOnAwake = false;
            m_player.audioOutputMode = audioOutput;
            m_player.renderMode = VideoRenderMode.RenderTexture;
            m_player.prepareCompleted += OnVideoPrepared;
        }

        private void CreateRendeTexture(int width, int height)
        {

            if (m_renderTexture == null ||
                    m_renderTexture.width != width ||
                    m_renderTexture.height != height)
            {
                if (m_renderTexture != null)
                {
                    m_renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(m_renderTexture);
                }
                m_renderTexture = new RenderTexture(width, height, 0);
                m_renderTexture.Create();
            }

        }


        private void LoadMediaClip(VideoClip source)
        {
            if (source == null) return;

            if (m_player == null)
                CreateMediaPlayer();

            CreateRendeTexture((int)source.width, (int)source.height);

            m_player.isLooping = looping;

            m_player.clip = source;
            m_player.targetTexture = m_renderTexture;
            m_player.Prepare();
        }

        private void LoadMediaURL(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            if (m_player == null)
                CreateMediaPlayer();

            m_player.isLooping = looping;

            m_player.url = url;
            m_player.targetTexture = m_renderTexture;
            m_player.Prepare();
        }

        private void OnVideoPrepared(VideoPlayer source)
        {


            this.style.backgroundImage = Background.FromRenderTexture(m_renderTexture);

            if (autoPlay)
                m_player.Play();

            OnVideoReady?.Invoke();
        }

        public void Play()
        {
            if (m_player == null || m_renderTexture == null || !m_player.isPrepared) return;

            m_player.Play();
        }

    }

}