using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GunMechanics : MonoBehaviour
{
    [SerializeField] private int MagSize;
    [SerializeField] private int MagCount;
    [SerializeField] private int Damage;
    [SerializeField] private AudioClip GunFireSound;
    [SerializeField] private AudioClip GunFireEmptySound;
    [SerializeField] private AudioClip GunReloadSound;
    [SerializeField] private Animation GunAnimation;
    [SerializeField] private GameObject Magazine;
    [SerializeField] private Text AmmoDisplayCurrent;
    [SerializeField] private Text AmmoDisplayMagazine;
    [SerializeField] private Camera WeaponCamera;

    private int Ammo;
    private bool MagEmpty = false;
    private bool Reloading = false;
    private bool MagDropped = false;
    private bool Aim = false;
    private int Fov = 90;

    void Start()
    {
        Ammo = MagSize;
        GunAnimation["GunReload"].layer = 1;
        GunAnimation["GunAim"].layer = 2;
    }

    void Update()
    {

        AmmoDisplayCurrent.text = "Ammo: " + Ammo;
        AmmoDisplayMagazine.text = "" + MagCount;

        Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, Fov, 0.1f);
        WeaponCamera.fieldOfView = Mathf.Lerp(WeaponCamera.fieldOfView, Fov, 0.1f);

        if (Input.GetButton("Fire2") && !GunAnimation.IsPlaying("GunReload") && !Input.GetButton("Fire3"))
        {
            if (Aim == false)
            {
                GunAnimation["GunAim"].speed = 1f;
                GunAnimation.Play("GunAim");
                Aim = true;
                Fov = 45;
            }
        }
        else if (Aim)
        {
            GunAnimation["GunAim"].speed = -1f;
            if (!GunAnimation.IsPlaying("GunAim"))
            {
                GunAnimation["GunAim"].time = 0.24f;
                GunAnimation.Play("GunAim");
            }
            Aim = false;
            Fov = 90;
        }

        if (Input.GetButtonDown("Fire"))
        {
            if (!GunAnimation.IsPlaying("GunReload") && !GunAnimation.IsPlaying("GunFire"))
            {
                if (Ammo == 0)
                {
                    AudioSource.PlayClipAtPoint(GunFireEmptySound, transform.position);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(GunFireSound, transform.position);
                    GunAnimation.Play("GunFire");
                    Ammo--;

                    RaycastHit shot;
                    if (Physics.Raycast(transform.parent.parent.position, transform.TransformDirection(Vector3.forward), out shot))
                    {
                        shot.transform.SendMessage("Damage", Damage, SendMessageOptions.DontRequireReceiver);
                    }

                    if (Ammo == 0)
                    {
                        GunAnimation["GunMagEmpty"].speed = 1f;
                        GunAnimation.Play("GunMagEmpty");
                        MagEmpty = true;
                    }
                }
            }
        }

        if (Input.GetButtonDown("Reload") && !GunAnimation.IsPlaying("GunReload"))
        {
            if (MagCount > 0)
            {
                AudioSource.PlayClipAtPoint(GunReloadSound, transform.position);
                GunAnimation.Play("GunReload");
                Ammo = MagSize;
                MagCount--;
                Reloading = true;
                MagDropped = true;
            }
        }

        if (Reloading && !GunAnimation.IsPlaying("GunReload"))
        {
            Reloading = false;
            if (MagEmpty)
            {
                MagEmpty = false;
                GunAnimation["GunMagEmpty"].speed = -1f;
                GunAnimation["GunMagEmpty"].time = 0.24f;
                GunAnimation.Play("GunMagEmpty");
            }
        }
        else if (MagDropped && (GunAnimation["GunReload"].time > 0.30f)) //temporary solution to spawning on "perfect" frame
        {
            MagDropped = false;
            GameObject mag = Instantiate(Magazine, Magazine.transform.position, Magazine.transform.rotation);
            mag.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            mag.AddComponent<MeshCollider>().convex = true;

            mag.AddComponent<Rigidbody>().mass = 0.1f;
            mag.GetComponent<Rigidbody>().velocity = transform.TransformDirection(new Vector3(-0.3f, -1, 0));
            mag.layer = 0;
        }

    }

}
