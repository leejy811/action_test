using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Camera followCamera;

    public GameObject[] Weapons;
    public bool[] hasWeapons;
    public GameObject[] grenades;
    public int hasGrenades;

    public float speed;
    public float ratioOfRunWalk;
    public float jumpPower;

    public int ammo;
    public int coin;
    public int health;

    public int maxAmmo;
    public int maxCoin;
    public int maxHealth;
    public int maxHasGrenades;

    float hAxis;
    float vAxis;
    float fireDelay;

    bool wDown;
    bool jDown;
    bool iDown;
    bool[] sDown = new bool[3];
    bool fDown;
    bool rDown;

    bool isDodge;
    bool isSwap;
    bool isReload;
    bool isFireReady = true;
    bool isBorder;

    Vector3 moveVec;
    Vector3 dodgeVec;

    Rigidbody rigid;
    Animator anim;

    GameObject nearObject;
    Weapon equipWeapon;
    int equipWeaponIndex = -1;
    void Start()
    {
        rigid = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

    }

    void Update()
    {
        GetInput();
        Move();
        Turn();
        Jump();
        Attack();
        Reload();
        Dodge();
        Interation();
        Swap();
    }

    void GetInput()
    {
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");
        wDown = Input.GetButton("Walk");
        jDown = Input.GetButtonDown("Jump");
        fDown = Input.GetButton("Fire1");
        rDown = Input.GetButtonDown("Reload");
        iDown = Input.GetButtonDown("Interation");
        sDown[0] = Input.GetButtonDown("swap1");
        sDown[1] = Input.GetButtonDown("swap2");
        sDown[2] = Input.GetButtonDown("swap3");
    }

    void Move()
    {
        moveVec = new Vector3(hAxis, 0, vAxis).normalized;

        if (isDodge)
            moveVec = dodgeVec;

        if (isSwap || !isFireReady || isReload)
            moveVec = Vector3.zero;

        if (!isBorder)
            transform.position += moveVec * speed * (wDown ? ratioOfRunWalk : 1f) * Time.deltaTime;

        MoveAnim();
    }

    void MoveAnim()
    {
        anim.SetBool("isRun", moveVec != Vector3.zero);
        anim.SetBool("isWalk", wDown);
    }

    void Turn()
    {
        transform.LookAt(transform.position + moveVec);

        if (fDown)
        {
            Ray ray = followCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit rayHit;
            if (Physics.Raycast(ray, out rayHit, 100))
            {
                Vector3 nextVec = rayHit.point - transform.position;
                nextVec.y = 0;
                transform.LookAt(nextVec + transform.position);
            }
        }
    }

    void Jump()
    {
        if (jDown && !IsJump() && !isDodge && moveVec == Vector3.zero && !isSwap)
        {
            rigid.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            anim.SetBool("isJump", true);
            anim.SetTrigger("doJump");
        }
    }

    bool IsJump()
    {
        return anim.GetBool("isJump");
    }

    void Attack()
    {
        if (equipWeapon == null)
            return;

        fireDelay += Time.deltaTime;
        isFireReady = equipWeapon.rate < fireDelay;

        if(fDown && isFireReady && !isDodge && !isSwap)
        {
            equipWeapon.Use();
            anim.SetTrigger(equipWeapon.type == Weapon.Type.Melee ? "doSwing" : "doShot");
            fireDelay = 0f;
        }
    }

    void Reload()
    {
        if (equipWeapon == null || equipWeapon.type == Weapon.Type.Melee || ammo == 0)
            return;

        if(rDown && !isDodge && !isSwap && !IsJump() && isFireReady)
        {
            anim.SetTrigger("doReload");
            isReload = true;

            Invoke("ReloadOut", 2f);
        }
    }

    void ReloadOut()
    {
        int reAmmo = ammo < equipWeapon.maxAmmo ? ammo : equipWeapon.maxAmmo;
        equipWeapon.curAmmo = reAmmo;
        ammo -= reAmmo;
        isReload = false;
    }

    void Dodge()
    {
        if (jDown && !IsJump() && !isDodge && moveVec != Vector3.zero && !isSwap)
        {
            dodgeVec = moveVec;
            speed *= 2;
            anim.SetTrigger("doDodge");
            isDodge = true;

            Invoke("DodgeOut", 0.4f);
        }
    }

    void DodgeOut()
    {
        speed *= 0.5f;
        isDodge = false;
    }

    void Swap()
    {
        if (sDown[0] && (!hasWeapons[0] || equipWeaponIndex == 0))
            return;
        if (sDown[1] && (!hasWeapons[1] || equipWeaponIndex == 1))
            return;
        if (sDown[2] && (!hasWeapons[2] || equipWeaponIndex == 2))
            return;

        int weaponIndex = -1;

        if (sDown[0]) weaponIndex = 0;
        if (sDown[1]) weaponIndex = 1;
        if (sDown[2]) weaponIndex = 2;

        if ((sDown[0] || sDown[1] || sDown[2]) && !IsJump() && !isDodge)
        {
            if (equipWeapon != null)
                equipWeapon.gameObject.SetActive(false);
            equipWeaponIndex = weaponIndex;
            equipWeapon = Weapons[weaponIndex].GetComponent<Weapon>();
            equipWeapon.gameObject.SetActive(true);

            anim.SetTrigger("doSwap");
            isSwap = true;
            Invoke("SwapOut", 0.4f);
        }
    }

    void SwapOut()
    {
        isSwap = false;
    }

    void Interation()
    {
        if(iDown && nearObject != null && !IsJump() && !isDodge && !isSwap)
        {
            Item item = nearObject.GetComponent<Item>();
            int WeaponIndex = item.value;
            hasWeapons[WeaponIndex] = true;

            Destroy(nearObject);
        }
    }

    void FixedUpdate()
    {
        FreezeRotation();
        StopToWall();
    }

    void FreezeRotation()
    {
        rigid.angularVelocity = Vector3.zero;
    }

    void StopToWall()
    {
        isBorder = Physics.Raycast(transform.position, transform.forward, 4f, LayerMask.GetMask("Wall"));
    }

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Floor")
        {
            anim.SetBool("isJump", false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Item")
        {
            Item item = other.GetComponent<Item>();
            switch (item.type)
            {
                case Item.Type.Ammo:
                    AssignItem(ref ammo, maxAmmo, item);
                    break;
                case Item.Type.Coin:
                    AssignItem(ref coin, maxCoin, item);
                    break;
                case Item.Type.Heart:
                    AssignItem(ref health, maxHealth, item);
                    break;
                case Item.Type.Grenade:
                    grenades[hasGrenades].SetActive(true);
                    AssignItem(ref hasGrenades, maxHasGrenades, item);
                    break;
            }

            Destroy(other.gameObject);
        }
    }

    void AssignItem(ref int hasItem, int maxHasItem, Item item)
    {
        hasItem += item.value;
        if (hasItem > maxHasItem)
            hasItem = maxHasItem;
    }

    void OnTriggerStay(Collider other)
    {
        if (other.tag == "Weapon")
            nearObject = other.gameObject;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag == "Weapon")
            nearObject = null;
    }
}
