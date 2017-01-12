using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Windows.Speech;

namespace Academy.HoloToolkit.Unity
{
    /// <summary>
    /// KeywordManager allows you to specify keywords and methods in the Unity
    /// Inspector, instead of registering them explicitly in code.
    /// This also includes a setting to either automatically start the
    /// keyword recognizer or allow your code to start it.
    ///
    /// IMPORTANT: Please make sure to add the microphone capability in your app, in Unity under
    /// Edit -> Project Settings -> Player -> Settings for Windows Store -> Publishing Settings -> Capabilities
    /// or in your Visual Studio Package.appxmanifest capabilities.
    /// </summary>
    public class KeywordManager : MonoBehaviour
    {
        [System.Serializable]
        public struct KeywordAndResponse
        {
            [Tooltip("The keyword to recognize.")]
            public string Keyword;

            [Tooltip("The UnityEvent to be invoked when the keyword is recognized.")]
            public UnityEvent Response;
        }

        // This enumeration gives the manager two different ways to handle the recognizer. Both will
        // set up the recognizer and add all keywords. The first causes the recognizer to start
        // immediately. The second allows the recognizer to be manually started at a later time.
        public enum RecognizerStartBehavior { AutoStart, ManualStart };

        [Tooltip("An enumeration to set whether the recognizer should start on or off.")]
        public RecognizerStartBehavior RecognizerStart;
        [Tooltip("An array of string keywords and UnityEvents, to be set in the Inspector.")]
        public KeywordAndResponse[] KeywordsAndResponses;

        private KeywordRecognizer keywordRecognizer;
        private Dictionary<string, UnityEvent> responses;

        void Start()
        {
            /// <summary>
            /// Convert the array of KeywordAndResponse structs into a dictionary for easy access
            /// Use this dict. to init. a KeywordRecognizer and attach an OnPhraseRecognized event handler
            /// to fire when the recognizer identifies a keyword.
            /// Also, start KeywordRecognizer if the start behavior is set to AutoStart
            /// </summary>
            if (KeywordsAndResponses.Length > 0)
            {
                // Convert the struct array into a dictionary, with the keywords as the keys and the methods as the values.
                // This helps easily link the keyword recognized to the UnityEvent to be invoked.
                // see http://stackoverflow.com/a/3611140 for explaination of the lambda expressions here (not very different)
                responses = KeywordsAndResponses.ToDictionary(keywordAndResponse => keywordAndResponse.Keyword,
                                                              keywordAndResponse => keywordAndResponse.Response);

                // KeywordRecognizer listens to speech input and attempts to match uttered phrases to a list of registered keywords.
                keywordRecognizer = new KeywordRecognizer(responses.Keys.ToArray());
                // attach handler for OnPhraseRecognized event
                keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;

                // if RegocnizerStart behavior set to auto, start the keywordRecognizer here
                if (RecognizerStart == RecognizerStartBehavior.AutoStart)
                {
                    keywordRecognizer.Start();
                }
            }
            else
            {
                Debug.LogError("Must have at least one keyword specified in the Inspector on " + gameObject.name + ".");
            }
        }

        void OnDestroy()
        {
            if (keywordRecognizer != null)
            {
                // stop the recognizer, unhook the event handler, and dispose of the resource
                StopKeywordRecognizer();
                keywordRecognizer.OnPhraseRecognized -= KeywordRecognizer_OnPhraseRecognized;
                keywordRecognizer.Dispose();
            }
        }

        /// <summary>
        /// TryGetValue from the dict. of keywords and method values for a recognized keyword 
        /// and invoke the asociated method for this key.
        /// </summary>
        /// <param name="args"></param>
        private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            UnityEvent keywordResponse;

            // Check to make sure the recognized keyword exists in the methods dictionary, then invoke the corresponding value/method.
            if (responses.TryGetValue(args.text, out keywordResponse))
            {
                keywordResponse.Invoke();
            }
        }

        /// <summary>
        /// Make sure the keyword recognizer is off and exists before trying to start it, then start it.
        /// Otherwise, leave it alone because it's already in the desired state.
        /// </summary>
        public void StartKeywordRecognizer()
        {
            if (keywordRecognizer != null && !keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Start();
            }
        }

        /// <summary>
        /// Make sure the keyword recognizer is on and exists, then stop it.
        /// Otherwise, leave it alone because it's already in the desired state.
        /// </summary>
        public void StopKeywordRecognizer()
        {
            if (keywordRecognizer != null && keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
            }
        }
    }
}