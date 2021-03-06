﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]

public class AI : NetworkBehaviour {
    public static AI Instance;
    [SerializeField] private GameObject regdollPreFab;

    //앞으로 달리기 및 걷기와 뒷걸음의 속도
    [SerializeField] private float forwardRunSpeed = 7.0f;
    [SerializeField] private float forwardSpeed = 3.0f;
    [SerializeField] private float sideSpeed = 2.0f;
    [SerializeField] private float backwardSpeed = 2.0f;

    //애니매이션 재생속도
    [SerializeField] private float animSpeed = 1.5f;

    private Animator anim;

    //캐릭터를 움직일 벡터
    private Vector3 velocity;
    float input_h;
    float input_v;

    private void Awake()
    {
        Instance = this;
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        StartCoroutine("Move");
    }

    private void FixedUpdate()
    {
        anim.SetFloat("Speed", input_v);

        //혹시 옆으로만 가고 있을 때에도 애니에 속도를 넣어줘야 해서.(수정예정)
        if (Mathf.Abs(input_v) <= 0.1f)
            anim.SetFloat("Speed", Mathf.Abs(input_h));
        anim.SetFloat("Direction", input_h);

        anim.speed = animSpeed;

        //애니매이션 상태 초기화
        anim.SetBool("Run", false);

        velocity = new Vector3(input_h, 0, input_v);

        #region Move
        
        //입력에 따라 속도의 변화를 준다
        if (Mathf.Abs(input_v) > 0.1f || Mathf.Abs(input_h) > 0.1f)
        {
            //뒤로 가고 있으면 어찌 되었건 뒤로가는 속도가 나오는 것으로 했다
            if (input_v < -0.1f)
                velocity *= backwardSpeed;

            //만약 옆으로 움직인다면
            else if (Mathf.Abs(input_h) > 0.1f)
            {
                //앞과 옆의 비율을 계산하여 옆속도 ~ 앞속도 만큼 움직인다
                velocity *= Mathf.Lerp(sideSpeed, forwardSpeed, input_v / (1 + Mathf.Abs(input_h)));
            }

            //앞으로만 가고 있다면
            else
            {
                //앞을 향하는 임계에 있을 경우만 달리기를 체크한다
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    anim.SetBool("Run", true);
                    velocity *= forwardRunSpeed;
                    anim.SetFloat("Speed", input_v * 2);
                }
                else
                    velocity *= forwardSpeed;
            }
        }

        //연산이 끝난 벡터를 로컬포지션에서 더한다
        transform.localPosition += velocity * Time.fixedDeltaTime;

        #endregion Move
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Att")
        {
            CmdDie();
        }
    }

    [Command]
    void CmdDie()
    {

        GameObject regdoll = Instantiate<GameObject>(regdollPreFab);
        regdoll.transform.position = this.transform.position += Vector3.down;
        //레그돌화

        //서버에도 소환
        NetworkServer.Spawn(regdoll);

        Destroy(this.gameObject);
    }

    IEnumerator Move()
    {
        while (true)
        {
            float wait = Random.Range(0, 3);

            input_h = Random.Range(-1, 2);
            input_v = Random.Range(-1, 2);

            var lookDirection = Quaternion.LookRotation(new Vector3(input_h, 0 , input_v));
            Debug.Log(lookDirection);
            this.transform.rotation = lookDirection;

            var ray = Physics.Raycast(this.transform.position, this.transform.position + new Vector3(input_h, 0, input_v) * 1, 0>>9);
            if (ray)
            {
                input_h = 0;
                input_v = 0;
                yield return new WaitForSeconds(wait);
                continue;
            }
            
            yield return new WaitForSeconds(wait);
        }
    }
}
