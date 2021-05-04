using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    public int aiType;
    public float baseSpeed;
    public float attackRange;
    public float attackWidth;
    public float visionRange;
    public GameObject spottedList;
    public GameObject playerHP;
    public Vector3 patrolPoint;


    private Animator animator;

    public enum eState
    {
        idle,
        fallowing,
        searching,
        attacking,
        patroling,
        afterAttack,
        goToPosition,
        attackCharge,
        attackShoot,
        stun,
        hiding,
        fallowingOther
    }

    GameObject player;
    eState currentState = eState.patroling;
    Vector3 startingPosition;

    eState prevState;
    Vector3 prevPosition;
    Vector3 lastPosition;
    int layerMask;
    RaycastHit2D hit, hit2;

    //fallow variables
    Vector3 fallowPos = new Vector3();

    //fallowing Others variables
    public bool playerSpotted;

    //patrolling variables
    Vector3 nextPoint;

    //search variables
    Vector3 positionToSearch = new Vector3(0, 0, 0);
    List<Vector3> path;
    bool timerRunning = false;
    float timer = 1f;
    int searchIndex;
    float searchStopCD = 1f;
    float searchDuration = 11f;
    bool isSearching = false;

    //attack variables
    bool isAttacking = false;
    LineRenderer lineRenderer;
    GameObject Player;
    float attackDuration;
    float playerPosX;
    float playerPosY;
    float posX;
    float posY;
    float distance2;
    float ratio;

    //charge variables
    Vector3 chargeToPoint;
    bool charging = false;

    //stun var
    float stunTime;
    bool stunned = false;

    //after attack variables
    float attackCD;
    bool afterAttackActive = false;

    //moving to position variables
    bool movingToPosition = false;
    Vector3 targetPosToGo;
    bool stateChangePossible = false;
    float timerGTP = 1f;

    //hiding variables
    bool hiding = false;
    Vector3 targetPosition, previousPosition;
    float hideTimer = 1f;

    void Start()
    {
        nextPoint = patrolPoint;
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");

        startingPosition = transform.position;

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.widthMultiplier = attackWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
        lineRenderer.sortingLayerName = "Player";
        lineRenderer.startColor = new Vector4(255, 0, 0, 150);

        alertOthers();
    }

    void FixedUpdate()
    {
        alertOthers();

        Debug.Log(gameObject.name + " " + currentState);

        layerMask = 1 << 9;
        layerMask = ~layerMask;

        hit = Physics2D.Raycast(transform.position, (player.transform.position - transform.position), visionRange, layerMask);
        hit2 = Physics2D.Raycast(transform.position, (player.transform.position - transform.position), attackRange, layerMask);


        Action();
        if (currentState == eState.idle || currentState == eState.patroling )
        {
            checkOthers();
        }

        Vector3 direction = transform.InverseTransformDirection(transform.position - lastPosition);
        lastPosition = transform.position;

        if (direction.x != 0 || direction.y != 0)
        {
            animator.SetFloat("moveX", direction.x);
            animator.SetFloat("moveY", direction.y);
            animator.SetBool("walking", true);
        }
        else
        {
            animator.SetBool("walking", false);
        }

    }

    public void Action()
    {
        if (currentState == eState.idle)
        {
            Idle();
        }
        else if (currentState == eState.fallowing)
        {
            Fallowing();
        }
        else if (currentState == eState.patroling)
        {
            Patrolling();
        }
        else if (currentState == eState.searching)
        {
            Searching();
        }
        else if (currentState == eState.attacking)
        {
            Attacking();
        }
        else if (currentState == eState.afterAttack)
        {
            AfterAttack();
        }
        else if (currentState == eState.goToPosition)
        {
            GoToPosition();
        }
        else if (currentState == eState.attackCharge)
        {
            Charge();
        }
        else if (currentState == eState.attackShoot)
        {
            Shoot();
        }
        else if (currentState == eState.stun)
        {
            Stun();
        }
        else if (currentState == eState.hiding)
        {
            Hide();
        }
    }

    private void Idle()
    {
        if (hit && hit.collider.tag == "Player")
        {
            if (hit2 && hit2.collider.tag == "Player")
            {
                currentState = eState.attacking;
            }
            else
            {
                currentState = eState.fallowing;
            }
        }
    }

    private void Fallowing()
    {
        timerGTP -= Time.deltaTime;
        Debug.Log(timerGTP);
        if (timerGTP < 0)
        {
            stateChangePossible = true;
        }

        positionToSearch = player.transform.position;
        isSearching = false;

        if (hit && hit.collider.tag == "Player")
        {
            if (hit2 && hit2.collider.tag == "Player" && stateChangePossible)
            {
                currentState = eState.attacking;
                stateChangePossible = false;
                timerGTP = 1f;
            }
            else
            {
                float t1 = 0;
                float tx = transform.position.x - player.transform.position.x;
                float ty = transform.position.y - player.transform.position.y;
                if (tx >= 0 && ty >= 0)
                {
                    t1 = Mathf.Atan(ty / tx);
                }
                else if (tx < 0 && ty > 0)
                {
                    t1 = Mathf.PI + Mathf.Atan(ty / tx);
                }
                else if (tx < 0 && ty < 0)
                {
                    t1 = Mathf.PI + Mathf.Atan(ty / tx);
                }
                else if (tx > 0 && ty < 0)
                {
                    t1 = Mathf.PI * 2 + Mathf.Atan(ty / tx);
                }

                float tau = (Random.Range(0, Mathf.PI / 8) - Mathf.PI / 16) + t1;
                float r = attackRange * .7f;
                float x = Mathf.Cos(tau) * r;
                float y = Mathf.Sin(tau) * r;

                fallowPos = new Vector3(player.transform.position.x + x, player.transform.position.y + y);

                transform.position = Vector3.MoveTowards(transform.position, fallowPos, baseSpeed * Time.deltaTime);
            }
        }
        else
        {
            stateChangePossible = false;
            timerGTP = 1f;
            prevState = eState.fallowing;
            currentState = eState.searching;
        }
    }

    private void Patrolling()
    {
        if (hit && hit.collider.tag == "Player")
        {
            if (hit2 && hit2.collider.tag == "Player")
            {
                currentState = eState.attacking;
            }
            else
            {
                currentState = eState.fallowing;
            }
        }
        else
        {
            if (this.transform.position == patrolPoint)
            {
                nextPoint = startingPosition;
            }
            else if (this.transform.position == startingPosition)
            {
                nextPoint = patrolPoint;
            }

            this.transform.position = Vector3.MoveTowards(this.transform.position, nextPoint, baseSpeed * .4f * Time.deltaTime);
        }
    }

    //finds last known location of Player
    //starts moving to that point
    //looks for Player at random close to point last known position
    private void Searching()
    {
        timerGTP -= Time.deltaTime;
        Debug.Log(timerGTP);
        if (timerGTP < 0)
        {
            stateChangePossible = true;
        }

        if (hit && hit.collider.tag == "Player" && stateChangePossible)
        {
            currentState = eState.fallowing;
            isSearching = false;
            prevState = eState.searching;
            searchDuration = 11f;
            searchStopCD = 1f;
            stateChangePossible = false;
            timerGTP = 1f;
        }
        else if (hit2 && hit2.collider.tag == "Player" && stateChangePossible)
        {
            currentState = eState.attacking;
            isSearching = false;
            prevState = eState.searching;
            searchDuration = 11f;
            searchStopCD = 1f;
            stateChangePossible = false;
            timerGTP = 1f;
        }


        if (isSearching == false)
        {
            isSearching = true;
            path = Pathfinding(this.transform.position, positionToSearch);
            searchIndex = 0;
        }

        if (this.transform.position != path[searchIndex])
        {
            this.transform.position = Vector2.MoveTowards(this.transform.position, path[searchIndex], baseSpeed * Time.deltaTime);
        }
        else if (this.transform.position == path[searchIndex] && searchIndex != (path.Count - 1))
        {
            searchIndex++;
        }
        else if (this.transform.position == path[searchIndex] && searchIndex == (path.Count - 1))
        {
            if (searchStopCD <= 0f)
            {
                searchStopCD = 1f;
                searchIndex++;
                for (int i = 0; i < 100; i++)
                {
                    Vector3 temp = new Vector3();
                    temp = nextRandomPoint();
                    RaycastHit2D hit3 = Physics2D.Raycast(transform.position, temp - transform.position);
                    if (hit3.collider == null)
                    {
                        path.Add(temp);
                        i = 100;
                    }
                }
            }
        }

        searchDuration -= Time.deltaTime;
        searchStopCD -= Time.deltaTime;

        if (searchDuration <= 0)
        {
            stateChangePossible = false;
            timerGTP = 1f;
            isSearching = false;
            targetPosToGo = startingPosition;
            currentState = eState.goToPosition;
            prevState = eState.searching;
            searchDuration = 11f;
            searchStopCD = 1f;
        }
    }

    //activates LineRenderer of length of attackRange when Player is in attack range
    //tracks the player and after 1 sec damage to Player is done
    //and AI state is set to afterAttack
    //if player distance > attack range go to fallow state
    //if player not in vision range go to searching
    private void Attacking()
    {
        if (!isAttacking)
        {
            attackDuration = 1f;
            lineRenderer.enabled = true;
            isAttacking = true;
        }

        playerPosX = player.transform.position.x;
        playerPosY = player.transform.position.y;
        posX = this.transform.position.x;
        posY = this.transform.position.y;

        distance2 = Mathf.Sqrt(((playerPosX - posX) * (playerPosX - posX)) + ((playerPosY - posY) * (playerPosY - posY)));
        ratio = attackRange * 1.5f / distance2;

        Vector3 position = new Vector3(((1 - ratio) * posX + ratio * playerPosX), ((1 - ratio) * posY + ratio * playerPosY), 0);
        lineRenderer.SetPosition(0, new Vector3(posX, posY, 0));
        lineRenderer.SetPosition(1, position);

        if (Vector3.Distance(this.transform.position, player.transform.position) <= attackRange * .3f)
        {
            this.transform.position = Vector3.MoveTowards(this.transform.position, player.transform.position, baseSpeed * -.75f * Time.deltaTime);
        }
        else if (Vector3.Distance(this.transform.position, player.transform.position) >= attackRange * .95f)
        {
            this.transform.position = Vector3.MoveTowards(this.transform.position, player.transform.position, baseSpeed * .75f * Time.deltaTime);
        }

        attackDuration -= Time.deltaTime;
        if (attackDuration <= 0)
        {
            lineRenderer.enabled = false;
            isAttacking = false;
            if (aiType == 1)
            {
                currentState = eState.attackCharge;
            }
            else if (aiType == 2)
            {
                currentState = eState.attackShoot;
            }
        }
    }


    //moves object to position at the end of attackRange
    //at the end of charge or if collision occurs object is unable to move for stun period
    private void Charge()
    {
        if (!charging)
        {
            chargeToPoint = (transform.position + (Vector3.Normalize(player.transform.position - transform.position)) * attackRange * 1.5f);
            charging = true;
        }

        if (charging)
        {
            transform.position = Vector3.MoveTowards(transform.position, chargeToPoint, baseSpeed * 6 * Time.deltaTime);
            if (transform.position == chargeToPoint)
            {
                charging = false;
                currentState = eState.afterAttack;
            }
        }
    }

    private void Shoot()
    {
        GameObject clone = (GameObject)Instantiate(Resources.Load("projectile"));
        clone.transform.position = transform.position;
        currentState = eState.hiding;
    }

    //prevents any actions for period of time
    private void Stun()
    {
        if (!stunned)
        {
            stunTime = 3f;
            stunned = true;
        }

        stunTime -= Time.deltaTime;

        if (stunTime <= 0)
        {
            stunned = false;
            targetPosToGo = startingPosition;
            prevState = eState.stun;

            if (hit && hit.collider.tag == "Player")
            {
                if (hit2 && hit2.collider.tag == "Player")
                {
                    currentState = eState.attacking;
                }
                else
                {
                    currentState = eState.fallowing;
                }
            }
            else
            {
                currentState = eState.goToPosition;
            }
        }
    }

    //activates 5sec cooldown after attack and moves away from player
    private void AfterAttack()
    {
        if (!afterAttackActive && aiType == 1)
        {
            prevPosition = this.transform.position;
            attackCD = 3f;
            afterAttackActive = true;
        }

        attackCD -= Time.deltaTime;
        this.transform.position = Vector3.MoveTowards(this.transform.position, player.transform.position, baseSpeed * -.7f * Time.deltaTime);

        if (afterAttackActive && attackCD <= 0)
        {
            if (hit && hit.collider.tag == "Player")
            {
                if (hit2 && hit2.collider.tag == "Player")
                {
                    currentState = eState.attacking;
                    afterAttackActive = false;
                }
                else
                {
                    currentState = eState.fallowing;
                    afterAttackActive = false;
                }
            }
            else
            {
                prevState = eState.afterAttack;
                positionToSearch = prevPosition;
                currentState = eState.searching;
                afterAttackActive = false;
            }
        }
    }


    //returns to starting position and goes to Idle state
    private void GoToPosition()
    {
        timerGTP -= Time.deltaTime;
        if(timerGTP < 0)
        {
            stateChangePossible = true;
        }

        if (hit && hit.collider.tag == "Player" && stateChangePossible)
        {
            currentState = eState.fallowing;
            movingToPosition = false;
            timerGTP = 1f;
            stateChangePossible = false;
        }
        else if (hit2 && hit2.collider.tag == "Player" && stateChangePossible)
        {
            currentState = eState.attacking;
            movingToPosition = false;
            timerGTP = 1f;
            stateChangePossible = false;
        }
        else
        {
            if (!movingToPosition)
            {
                path = Pathfinding(transform.position, targetPosToGo);
                movingToPosition = true;
                searchIndex = 0;
            }

            transform.position = Vector2.MoveTowards(transform.position, path[searchIndex], baseSpeed * Time.deltaTime);

            if (transform.position == path[searchIndex] && searchIndex != (path.Count - 1))
            {
                searchIndex++;
            }

            if (searchIndex == path.Count - 1 && transform.position == path[searchIndex])
            {
                movingToPosition = false;
                timerGTP = 1f;
                stateChangePossible = false;
                if (prevState == eState.stun)
                {
                    currentState = eState.patroling;
                }
                else if (prevState == eState.hiding)
                {
                    currentState = eState.searching;
                }
                else if (prevState == eState.searching)
                {
                    currentState = eState.patroling;
                }
                else
                {
                    Debug.Log("Error GoToPos");
                }
            }
        }
    }

    private void Hide()
    {
        float posx, posy;
        int layerMask = 1 << 9;
        layerMask = ~layerMask;

        if (!hiding)
        {
            for (int i = 1; i < 4; i++)
            {
                for (int j = 0; j < 500; j++)
                {
                    posx = transform.position.x + ((Random.Range(-1f, 1f) * i));
                    posy = transform.position.y + ((Random.Range(-1f, 1f) * i));
                    RaycastHit2D hit22 = Physics2D.Raycast(transform.position, (new Vector3(posx, posy) - transform.position), i, layerMask);
                    RaycastHit2D hit11 = Physics2D.Raycast(new Vector3(posx, posy), (player.transform.position - new Vector3(posx, posy)), visionRange, layerMask);
                    if (hit11 && hit11.collider.tag != "Player" && hit22.collider == null)
                    {
                        previousPosition = transform.position;
                        targetPosition = new Vector3(posx, posy);
                        hiding = true;
                        j = 500;
                        i = 4;
                    }
                }
            }
        }

        this.transform.position = Vector3.MoveTowards(transform.position, targetPosition, baseSpeed * 1.5f * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPosition) < 0.2f && hiding)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0)
            {
                for (int i = 1; i < 5; i++)
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        posx = transform.position.x + ((Random.Range(-1f, 1f) * i));
                        posy = transform.position.y + ((Random.Range(-1f, 1f) * i));
                        RaycastHit2D hit22 = Physics2D.Raycast(transform.position, (new Vector3(posx, posy) - transform.position), i, layerMask);
                        RaycastHit2D hit11 = Physics2D.Raycast(new Vector3(posx, posy), (player.transform.position - new Vector3(posx, posy)), visionRange, layerMask);
                        if (hit11 && hit22 && hit22.collider == null && hit11.collider.tag == "Player" && hit11.collider.tag != "Enviroment")
                        {
                            targetPosition = new Vector3(posx, posy) * 1.05f;
                            hiding = true;
                            j = 1000;
                            i = 5;
                        }
                    }
                }
                hiding = false;
                targetPosToGo = targetPosition;
                prevState = eState.hiding;
                currentState = eState.goToPosition;
                hideTimer = 1f;
            }
        }
    }

    //simple pathfinding algorytm
    //problem with no-exit situations
    private List<Vector3> Pathfinding(Vector3 startPosition, Vector3 goalPosition)
    {
        List<Vector3> path = new List<Vector3>();
        List<Vector3> possibleMoves = new List<Vector3>();

        float bestDistance = 10000f;
        int bestDistanceIndex = 0;
        Vector3 previousMove = startPosition;

        Vector3 currentPosition = startPosition;
        path.Add(startPosition);

        RaycastHit2D dot;
        for (int j = 0; j <= 1000; j++)
        {
            dot = Physics2D.Raycast(currentPosition, (new Vector3(currentPosition.x + 1f, currentPosition.y + 1f)) - currentPosition, 2f);
            if (dot.collider == null)
            {
                possibleMoves.Add(new Vector3(currentPosition.x + 1f, currentPosition.y + 1f, 0));
            }
            dot = Physics2D.Raycast(currentPosition, (new Vector3(currentPosition.x + 1f, currentPosition.y)) - currentPosition, 2f);
            if (dot.collider == null)
            {
                possibleMoves.Add(new Vector3(currentPosition.x + 1f, currentPosition.y, 0));
            }
            dot = Physics2D.Raycast(currentPosition, (new Vector3(currentPosition.x + 1f, currentPosition.y - 1f)) - currentPosition, 2f);
            if (dot.collider == null)
            {
                possibleMoves.Add(new Vector3(currentPosition.x + 1f, currentPosition.y - 1f, 0));
            }
            dot = Physics2D.Raycast(currentPosition, (new Vector3(currentPosition.x, currentPosition.y + 1f)) - currentPosition, 2f);
            if (dot.collider == null)
            {
                possibleMoves.Add(new Vector3(currentPosition.x, currentPosition.y + 1f, 0));
            }
            dot = Physics2D.Raycast(currentPosition, (new Vector3(currentPosition.x, currentPosition.y - 1f)) - currentPosition, 2f);
            if (dot.collider == null)
            {
                possibleMoves.Add(new Vector3(currentPosition.x, currentPosition.y - 1f, 0));
            }
            dot = Physics2D.Raycast(currentPosition, (new Vector3(currentPosition.x - 1f, currentPosition.y + 1f)) - currentPosition, 2f);
            if (dot.collider == null)
            {
                possibleMoves.Add(new Vector3(currentPosition.x - 1f, currentPosition.y + 1f, 0));
            }
            dot = Physics2D.Raycast(currentPosition, (new Vector3(currentPosition.x - 1f, currentPosition.y)) - currentPosition, 2f);
            if (dot.collider == null)
            {
                possibleMoves.Add(new Vector3(currentPosition.x - 1f, currentPosition.y, 0));
            }
            dot = Physics2D.Raycast(currentPosition, (new Vector3(currentPosition.x - 1f, currentPosition.y - 1f)) - currentPosition, 2f);
            if (dot.collider == null)
            {
                possibleMoves.Add(new Vector3(currentPosition.x - 1f, currentPosition.y - 1f, 0));
            }

            for (int i = 0; i <= possibleMoves.Count - 1; i++)
            {
                float temp = Vector3.Distance(possibleMoves[i], goalPosition);
                if (temp < bestDistance && previousMove != possibleMoves[i])
                {
                    // Debug.Log(possibleMoves.Count);
                    bestDistance = temp;
                    bestDistanceIndex = i;
                }
            }

            previousMove = currentPosition;
            path.Add(possibleMoves[bestDistanceIndex]);
            // Debug.DrawLine(currentPosition, possibleMoves[bestDistanceIndex], Color.black, 100f);
            currentPosition = possibleMoves[bestDistanceIndex];
            possibleMoves.Clear();
            bestDistance = 10000f;
            bestDistanceIndex = 0;
            if (Vector3.Distance(currentPosition, goalPosition) < 1f)
            {
                j = 1000;
            }
        }

        // optimalisation of path


        int realCount = path.Count - 1;
        for (int i = 0; i <= realCount - 2; i++)
        {
            for (int j = 0; j <= realCount - 2; j++)
            {
                dot = Physics2D.Raycast(path[realCount - i], (path[j] - path[realCount - i]), Vector3.Distance(path[realCount - i], path[j]));
                if (!dot.collider)
                {
                    path.RemoveRange(j + 1, (realCount - i) - j - 1);
                    j = realCount + 2;
                }
                realCount = path.Count - 1;
            }
        }

        List<Vector3> path2 = new List<Vector3>();
        path2.Add(path[0]);
        for (int i = 0; i < path.Count - 1; i++)
        {
            int temp = 0;
            for (int j = 0; j < 10; j++)
            {
                Vector3 point = path[i] + ((path[i + 1] - path[i]) / 10) * j;
                if (Physics2D.OverlapCircle(point, 1) && Physics2D.OverlapCircle(point, 1).tag == "Enviroment")
                {
                    Vector3 point2 = Physics2D.ClosestPoint(point, Physics2D.OverlapCircle(point, 1));
                    path2.Add(point - Vector3.Normalize(point2 - point));
                    temp = 1;
                }
                else if (j == 9 && temp == 0)
                {
                    path2.Add(path[i + 1]);
                }
            }
        }

        if (Physics2D.OverlapCircle(path[path.Count - 1], 1) && Physics2D.OverlapCircle(path[path.Count - 1], 1).tag == "Enviroment")
        {
            Vector3 point2 = Physics2D.ClosestPoint(path[path.Count - 1], Physics2D.OverlapCircle(path[path.Count - 1], 1));
            path2.Add(path[path.Count - 1] - Vector3.Normalize(point2 - path[path.Count - 1]));
        }
        else
        {
            path2.Add(path[path.Count - 1]);
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            Debug.DrawLine(path[i], path[i + 1], Color.red, 2f);
        }
        for (int i = 0; i < path2.Count - 1; i++)
        {
            Debug.DrawLine(path2[i], path2[i + 1], Color.green, 2f);
        }


        //List<Vector3> path2 = new List<Vector3>();
        //for (int i = 0; i < path.Count - 1; i++)
        //{
        //    path2.Add(path[i]);
        //    Vector3 point = path[i] + ((path[i + 1] - path[i]) / 2);
        //    if (Physics2D.OverlapCircle(point, 1) && Physics2D.OverlapCircle(point, 1).tag == "Enviroment")
        //    {
        //        Vector3 point2 = Physics2D.ClosestPoint(point, Physics2D.OverlapCircle(point, 1));
        //        path2.Add(point - Vector3.Normalize(point2 - point));
        //    }
        //}

        //if (Physics2D.OverlapCircle(path[path.Count - 1], 1) && Physics2D.OverlapCircle(path[path.Count - 1], 1).tag == "Enviroment")
        //{
        //    Vector3 point2 = Physics2D.ClosestPoint(path[path.Count - 1], Physics2D.OverlapCircle(path[path.Count - 1], 1));
        //    path2.Add(path[path.Count - 1] - Vector3.Normalize(point2 - path[path.Count - 1]));
        //}
        //else
        //{
        //    path2.Add(path[path.Count - 1]);
        //}

        //for (int i = 0; i < path.Count - 1; i++)
        //{
        //    Debug.DrawLine(path[i], path[i + 1], Color.red, 2f);
        //}
        //for (int i = 0; i < path2.Count - 1; i++)
        //{
        //    Debug.DrawLine(path2[i], path2[i + 1], Color.green, 2f);
        //}


        return path2;
    }

    private void alertOthers()
    {
        if (hit && hit.collider.tag == "Player")
        {
            playerSpotted = true;
        }
        else if (hit && hit.collider.tag != "Player")
        {
            playerSpotted = false;
        }
    }

    private void checkOthers()
    {
        List<GameObject> list = spottedList.GetComponent<signalSpotted>().listSpotted;
        for (int i = 0; i <= list.Count - 1; i++)
        {
            if (hit)
            {
                positionToSearch = list[i].transform.position;
                prevState = currentState;
                currentState = eState.searching;
            }
        }
    }

    private Vector3 nextRandomPoint()
    {
        Vector3 temp = new Vector3(transform.position.x + (Random.Range(-1, 1)), transform.position.y + (Random.Range(-1, 1)));
        return temp;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (charging && collision.gameObject)
        {
            transform.position = transform.position;
            charging = false;
            currentState = eState.stun;
            if (collision.gameObject.tag == "Player")
            {
                playerHP.GetComponent<playerHP>().sethp(-1);
            }
        }
    }
}
