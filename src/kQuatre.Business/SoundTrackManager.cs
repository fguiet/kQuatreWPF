using fr.guiet.kquatre.business.Exceptions;
using NAudio.Wave;
using System;
using System.IO;

namespace fr.guiet.kquatre.business
{
    public class SoundTrackManager
    {
        #region Private 

        //SoundTrack
        private WaveOutEvent _outputDevice = null;
        private AudioFileReader _audioFile = null;
        private string _soundTrackFilePath = string.Empty;

        #endregion        

        public string SoundTrackFilePath
        {
            get
            {
                return _soundTrackFilePath;                
            }

            set
            {
                if (_soundTrackFilePath != value)
                {
                    _soundTrackFilePath = value;                    
                }
            }
        }

        public bool IsSoundTrackSanityCheckOk()
        {
            return _audioFile != null && _outputDevice != null;
        }

        public bool HasSoundTrackToPlay()
        {
            return !string.IsNullOrEmpty(_soundTrackFilePath);
        }

        public void Play()
        {
            _outputDevice.Play();
        }

        public void Stop()
        {
            _outputDevice.Stop();

            //Reset Audio file to beginning
            _audioFile.CurrentTime = new TimeSpan(0, 0, 0);
        }

        /// <summary>
        /// Return true if soundtrack has been loaded sucessfully
        /// </summary>
        /// <param name="soundTrackFilePath"></param>
        /// <returns></returns>
        public bool Load(string soundTrackFilePath)
        {
            //Dispose of old object if user is loading new soundtrack
            Dispose();

            _soundTrackFilePath = soundTrackFilePath;

            if (!String.IsNullOrEmpty(_soundTrackFilePath))
            {
                //Double check SoundTrack exists !
                if (File.Exists(_soundTrackFilePath))
                {
                    try
                    {
                        //Test whether Init device is working
                        InitDevice();
                        return true;
                    }
                    catch
                    {
                        Dispose();
                        return false;
                    }                    
                }
            }

            return false;
        }

        private void Dispose()
        {
            _soundTrackFilePath = string.Empty;

            if (_outputDevice != null)
            {
                _outputDevice.Dispose();
                _outputDevice = null;
            }

            if (_audioFile != null)
            {
                _audioFile.Dispose();
                _audioFile = null;
            }
        }

        private void InitDevice()
        {
            try
            {
                //Let's Play some music
                if (_outputDevice == null)
                {
                    _outputDevice = new WaveOutEvent();
                }

                if (_audioFile == null)
                {
                    _audioFile = new AudioFileReader(_soundTrackFilePath);
                    _outputDevice.Init(_audioFile);
                }
            }
            catch (Exception ex)
            {
                throw new NotPlayableSoundTrackException(string.Format("Le fichier {0} n'est pas lisible par cette appareil (problème de carte son ?). Exception : {1}", _soundTrackFilePath, ex.Message));
            }
        }      
    }
}
