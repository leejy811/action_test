using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class Enemy : MonoBehaviour
{
    public int maxHealth;
    public int curHealth;
    public Transform target;
    public bool isChase;

    Rigidbody rigid;
    BoxCollider boxCollider;
    Material mat;
    NavMeshAgent nav;
    Animator anim;

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        mat = GetComponentInChildren<MeshRenderer>().material;
        anim = GetComponentInChildren<Animator>();
        nav = GetComponent<NavMeshAgent>();

        Invoke("ChaseStart", 2);
    }

    void ChaseStart()
    {
        isChase = true;
        anim.SetBool("isWalk", true);
    }

    void Update()
    {
        if (isChase)
            nav.SetDestination(target.position);
    }

    void FixedUpdate()
    {
       FreezeVelocity();
    }

    void FreezeVelocity()
    {
        if (isChase)
        {
            rigid.angularVelocity = Vector3.zero;
            rigid.velocity = Vector3.zero;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Melee")
        {
            Weapon weapon = other.GetComponent<Weapon>();
            curHealth -= weapon.damage;

            Vector3 reactVec = transform.position - other.transform.position;
            StartCoroutine(OnDamage(reactVec, false));
        }
        else if(other.tag == "Bullet")
        {
            Bullet bullet = other.GetComponent<Bullet>();
            curHealth -= bullet.damage;

            Vector3 reactVec = transform.position - other.transform.position;
            Destroy(other.gameObject);
            StartCoroutine(OnDamage(reactVec, false));
        }
    }

    public void HitByGrenade(Vector3 explosionPos)
    {
        curHealth -= 100;
        Vector3 reactVec = transform.position - explosionPos;
        StartCoroutine(OnDamage(reactVec, true));
    }

    IEnumerator OnDamage(Vector3 reactVec, bool isGrenade)
    {
        mat.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        if(curHealth > 0)
        {
            mat.color = Color.white;
        }
        else
        {
            mat.color = Color.gray;
            gameObject.layer = 12;

            isChase = false;
            nav.enabled = false;
            anim.SetTrigger("doDie");

            if (isGrenade)
            {
                rigid.freezeRotation = false;
                float explosionPower = (reactVec.magnitude - 20) * -0.5f;
                rigid.AddForce((reactVec.normalized + Vector3.up * 3) * explosionPower, ForceMode.Impulse);
                rigid.AddTorque((reactVec.normalized + Vector3.up * 3) * explosionPower * 3f, ForceMode.Impulse);
                Destroy(gameObject, 4f);
            }
            else
            {
                rigid.AddForce((reactVec.normalized + Vector3.up) * 5, ForceMode.Impulse);
                Destroy(gameObject, 4f);
            }
        }
    }
}
