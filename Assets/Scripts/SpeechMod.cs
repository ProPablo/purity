using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeechMod : MonoBehaviour
{
    // Start is called before the first frame update
    public int delay = 500; 
    void Start()
    {
        foreach (var device in Microphone.devices)
        {
            Debug.Log("Name: " + device);
        }

        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.clip = Microphone.Start(null, true, 150, 44100);
        audioSource.loop = true;
        while (!(Microphone.GetPosition(null) > delay))
        {
            
        }
        audioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        //Have ffted version ready and pitch shift based on current note and beat, inverse fft and play 
    }
}