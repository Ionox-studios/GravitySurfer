using System.Collections.Generic;
using System.Collections;
using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class Speedometer : MonoBehaviour
{
    public float maxSpeed = 0.0f; // max speed of target in km/h
    public Rigidbody target;

    [Header("UI")]
    public TextMeshProUGUI speedLabel; // label that displays speed
    private float speed = 0.0f;

    private void Update()
    {
        speed = target.linearVelocity.magnitude * 100f; //3.6 is the conversion
        if (speedLabel != null)
           speedLabel.text = ((int)speed + "");

    }
}
