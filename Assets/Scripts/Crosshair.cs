using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{

    [SerializeField] private GameObject CrosshairTop;
    [SerializeField] private GameObject CrosshairBottom;
    [SerializeField] private GameObject CrosshairLeft;
    [SerializeField] private GameObject CrosshairRight;
    [SerializeField] private Animation GunAnimation;

    private int Crouch = 20;
    private int Stand = 30;
    private int Moving = 20;
    private int Sprint = 15;
    private int Fire = 30;

    private int Target;

    private float Distance = 30;
    private int Speed = 0;

    private Color ColorTop, ColorBottom, ColorLeft, ColorRight;
    private float Opacity = 1;
    private float OpacityAdd = 1;


    void Start()
    {
        ColorTop = CrosshairTop.GetComponent<RawImage>().color;
        ColorBottom = CrosshairBottom.GetComponent<RawImage>().color;
        ColorLeft = CrosshairLeft.GetComponent<RawImage>().color;
        ColorRight = CrosshairRight.GetComponent<RawImage>().color;
    }

    void Update()
    {
        Target = 0;
        Speed = 0;

        if (Input.GetButton("Fire2") && !Input.GetButton("Fire3"))
        {
            Distance = MoveCrosshair(Distance, 10, -100);
            CrossHairOpacityAdd(-10);
        }
        else
        {
            CrossHairOpacityAdd(10);

            if (UnityStandardAssets.Characters.FirstPerson.FirstPersonController.m_MoveDir != new Vector3(0, -10, 0))
            {
                Speed += 60;
                Target += Moving;

                if (Input.GetButton("Fire3"))
                {
                    Speed += 20;
                    Target += Sprint;
                }
            }

            if (GunAnimation.IsPlaying("GunFire"))
            {
                Speed += 100;
                Target += Fire;
            }

            //if stand or crouch
            Target += Stand;

            if (Distance > Target)
            {
                Distance = MoveCrosshair(Distance, Target, -40);
            }
            else if (Distance < Target)
            {
                if (Speed == 0)
                {
                    Speed = 100;
                }

                Distance = MoveCrosshair(Distance, Target, Speed);
            }
        }

        CrosshairOpacity();

        CrosshairTop.transform.position = new Vector3(transform.position.x, transform.position.y + Distance, transform.position.z);
        CrosshairBottom.transform.position = new Vector3(transform.position.x, transform.position.y - Distance, transform.position.z);
        CrosshairLeft.transform.position = new Vector3(transform.position.x - Distance, transform.position.y, transform.position.z);
        CrosshairRight.transform.position = new Vector3(transform.position.x + Distance, transform.position.y, transform.position.z);

    }

    float MoveCrosshair(float curr, int target, int speed)
    {
        curr += Time.deltaTime * speed;

        if ((curr < target) && (speed < 0))
        {
            curr = target;
        }
        else if ((curr > target) && (speed > 0))
        {
            curr = target;
        }

        return curr;
    }

    void CrosshairOpacity()
    {
        if (Distance <= 30)
        {
            Opacity = 1;
        }
        else
        {
            Opacity = 1 - ((Distance - 30) / (Moving + Fire + Sprint)) * 0.5f;
        }

        ColorTop.a = ColorBottom.a = ColorLeft.a = ColorRight.a = Opacity * OpacityAdd;

        CrosshairTop.GetComponent<RawImage>().color = ColorTop;
        CrosshairBottom.GetComponent<RawImage>().color = ColorBottom;
        CrosshairLeft.GetComponent<RawImage>().color = ColorLeft;
        CrosshairRight.GetComponent<RawImage>().color = ColorRight;

    }

    void CrossHairOpacityAdd(int speed)
    {
        OpacityAdd += Time.deltaTime * speed;

        if (OpacityAdd < 0)
        {
            OpacityAdd = 0;
        }
        else if (OpacityAdd > 1)
        {
            OpacityAdd = 1;
        }
    }
}
