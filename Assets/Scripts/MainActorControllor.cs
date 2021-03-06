﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collision))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class MainActorControllor : MonoBehaviour {
    public ToolboxController toolbox;
    public Ground ground;

    Rigidbody rb;
    Animator anime;
    //Transform preTran;
    AudioSource audio;
    bool is_jumping = false;
    Vector3 mouseInitial;
    ItemCtrl live = new ItemCtrl(Const.GameItemID.Empty);
    float lastJumpTime;
    float lastClickTime;
    float lastSpaceReleaseTime;
    enum SpaceMode { Jump, Flying};
    SpaceMode spaceMode = SpaceMode.Jump;
    void Start () {
        Cursor.visible = false;
        audio = transform.GetComponent<AudioSource>();
        anime = transform.GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        mouseInitial = Input.mousePosition;
        lastJumpTime = Time.time;
        lastClickTime = Time.time;
        lastSpaceReleaseTime = Time.time;
        while (!Ground.mapReady) StartCoroutine(wait());
        transform.position = Ground.getPointOnGround(new Vector3(Const.mapSize.x/2, 0, Const.mapSize.z/2));
    }
	IEnumerator wait()
    {
        yield return new WaitForSeconds(0.1f);
    }
	// Update is called once per frame
	void Update () {
        anime.SetFloat("speed", 0f);
        // Move
        #region Move
        if (Input.GetKey(KeyCode.W)) {
            transform.localPosition += Const.moveSpeed * Time.deltaTime * transform.forward;
        }
        else if (Input.GetKey(KeyCode.S)) {
            transform.localPosition += -1 * Const.moveSpeed * Time.deltaTime * transform.forward;
        }
        if (Input.GetKey(KeyCode.D)) {
            transform.localPosition += Const.moveSpeed * Time.deltaTime * transform.right;
        }
        else if (Input.GetKey(KeyCode.A)) {
            transform.localPosition += -1 * Const.moveSpeed * Time.deltaTime * transform.right;
        }
        anime.SetFloat("speed", rb.velocity.magnitude);
        if(transform.position.y  < Const.mapOrigin.y)
            transform.position = Ground.getPointOnGround(new Vector3(Const.mapSize.x / 2, 0, Const.mapSize.z / 2));
        #endregion
        // Jump
        #region Jump
        if (Input.GetKey(KeyCode.Space)) {
            if (clickEvent.modestate && Time.time - lastSpaceReleaseTime < 1) {
                Debug.Log("mode changed" + spaceMode.ToString());
                if (spaceMode == SpaceMode.Jump) {
                    spaceMode = SpaceMode.Flying;
                    transform.GetComponent<Rigidbody>().useGravity = false;
                }
                else if (spaceMode == SpaceMode.Flying) {
                    spaceMode = SpaceMode.Jump;
                    transform.GetComponent<Rigidbody>().useGravity = true;
                }
            }
            if(spaceMode == SpaceMode.Jump) {
                if (!is_jumping && Time.time - lastJumpTime > 0.5) {
                    //rb.AddForce(jumpForce * Time.deltaTime * Vector3.up, ForceMode.Impulse);
                    is_jumping = true;
                    lastJumpTime = Time.time;
                    rb.AddForce(new Vector3(0, 7, 0), ForceMode.Impulse);
                    //transform.localPosition += jumpForce * Time.deltaTime * Vector3.up;
                }
            }
            else if(spaceMode == SpaceMode.Flying) {
                transform.localPosition += Const.moveSpeed * Time.deltaTime * transform.up;
            }
        }
        else
            lastSpaceReleaseTime = Time.time;

        if (Input.GetKey(KeyCode.LeftShift) && spaceMode == SpaceMode.Flying)
            transform.localPosition -= Const.moveSpeed * Time.deltaTime * transform.up;
        #endregion
        // Rotate
        #region Rotate
        Vector3 dis = Input.mousePosition - mouseInitial;
        transform.localEulerAngles = new Vector3(0, Const.rotateSpeed*dis.x, 0);
        Transform cam = transform.Find("Main Camera");
        float rotate = Mathf.Abs(-1*Const.updownSpeed * dis.y)>90 ? (dis.y > 0 ? -90 : 90) : -1 * Const.updownSpeed * dis.y;
        cam.transform.localEulerAngles = new Vector3(rotate, 0, 0);
        #endregion
        // Click
        #region MouseClick
        if (Input.GetMouseButton(0)) {
            //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2));
            RaycastHit rch;
            if (Physics.Raycast(ray, out rch)) {
                Const.GameItemID hitId = ItemMap.getItemsID(rch.transform.gameObject.name);
                //Debug.Log("Hit= "+ hitId.ToString());
                int instanceId = rch.transform.gameObject.GetInstanceID();
                // Cube
                if (ItemMap.isItem(hitId)) {
                    if (!audio.isPlaying)
                        audio.Play(0);
                    int destroyLevel = live.isAlive(hitId, instanceId, Const.attackPower * Time.deltaTime);
                    /*if(destroyLevel == -1) {
                        Material breakM1 = (Material)Resources.Load(ItemMap.getTextureName(hitId));
                        preTran.GetComponent<Renderer>().material = breakM1;
                    }*/
                    if (destroyLevel <= 0) {
                        toolbox.pushItem(hitId);
                        Destroy(rch.transform.gameObject);
                    }
                    else {
                        string name = "destroy/Materials/destroy_stage_" + (9 - destroyLevel);
                        Material[] breakM1 = new Material[2];
                        breakM1[0] = (Material)Resources.Load(ItemMap.getTextureName(hitId));
                        breakM1[1] = (Material)Resources.Load(name);
                        rch.transform.GetComponent<Renderer>().materials = breakM1;
                    }
                    //preTran = rch.transform;
                }
                // Creature
                else {
                    rch.transform.GetComponent<LiveManager>().attack(Const.attackPower);// * Time.deltaTime);
                    rch.transform.GetComponent<Rigidbody>().AddForce(ray.direction.normalized*2000);
                }
            }
        }
        if (Input.GetMouseButton(1)) {
            if (toolbox.isSelected() && Time.time - lastClickTime > 0.5) {
                Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
                RaycastHit rch;
                if (Physics.Raycast(ray, out rch)) {
                    Vector3 target = rch.point + rch.normal/2;
                    target.x = Mathf.Round(target.x);
                    target.y = Mathf.Round(target.y);
                    target.z = Mathf.Round(target.z);
                    ground.instantiateItem(toolbox.deleteSeletedItem(), target);
                }
                lastClickTime = Time.time;
            }
        }
#endregion
    }
    void OnCollisionEnter(Collision other)
    {
        if(Vector3.Angle(other.contacts[0].normal, Vector3.up)<10)
            is_jumping = false;
    }
}
