using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class projectile : MonoBehaviour
{
    public Vector3 goal;
    private GameObject player;
    float timer = 3f;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        goal = (Vector3.Normalize(player.transform.position - transform.position))*Time.deltaTime * 11;

    }
    // Update is called once per frame
    void Update()
    {
        transform.Translate(goal);
        timer -= Time.deltaTime;
        if(timer <= 0)
        {
            Destroy(this.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            GameObject temp = GameObject.Find("player hp");
            temp.GetComponent<playerHP>().sethp(-1);
            Destroy(this.gameObject);
        }
        else if(collision.gameObject.tag != "Enemy")
        {
            Destroy(this.gameObject);
        }
    }
}
