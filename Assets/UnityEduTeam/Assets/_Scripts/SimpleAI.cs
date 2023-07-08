using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class SimpleAI : MonoBehaviour {
    [Header("Agent Field of View Properties")]
    public float viewRadius;
    public float viewAngle;

    public LayerMask playerMask;
    public LayerMask obstacleMask;

    [Space(5)]
    [Header("Agent Properties")]
    public float runSpeed;
    public float walkSpeed;
    public float patrolRadius;
    public float timeSinceLastCalled;

    private NavMeshAgent agent;
    private Animator anim;
    GameObject player;

    private Transform playerTarget;

    private Vector3 currentDestination;
    private GameObject[] randomPoints;
    private Vector3[] randomVectors;

    private bool playerSeen;

    private enum State {Wandering, Chasing};
    private State currentState;


    // Use this for initialization
    void Start ()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        randomPoints = GameObject.FindGameObjectsWithTag("RandomPoint");
        randomVectors = new Vector3[randomPoints.Length];
        CovertGameObjectsArrayToVector3Array();
        currentState = State.Wandering;
//        currentDestination = RandomPosInMaze(randomVectors);

        //currentDestination = RandomNavSphere(transform.position, patrolRadius, -1);
    }

    private void CovertGameObjectsArrayToVector3Array()
    {
        for (int i = 0; i < randomPoints.Length; i++)
        {
            randomVectors[i] = randomPoints[i].transform.position;
        }
    }

    private void CheckState()
    {
        FindVisibleTargets();

        switch(currentState)
        {
            case State.Chasing:
                ChaseBehavior();
                break;

            default:
                WanderBehavior();
                break;

        }
    }

    void WanderBehavior()
    {
        anim.SetTrigger("walk");
        agent.speed = walkSpeed;

        float dist = agent.remainingDistance;

        if (dist <2 || agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            // create random patrol sphere
            // which we will try to replace with 4 points around the maze that we feed in at random.
            //currentDestination = RandomNavSphere(transform.position, patrolRadius, -1);
            timeSinceLastCalled += Time.deltaTime;
            if(timeSinceLastCalled >= 4)
            {
                timeSinceLastCalled = 0; // could replace this with random number
                currentDestination = RandomPosInMaze(randomVectors);
                agent.SetDestination(currentDestination);
            }
        }

    }
    private Vector3 RandomPosInMaze(Vector3[] randomPoints)
    {
        return randomPoints[UnityEngine.Random.Range(0, randomPoints.Length)];
    }

    void ChaseBehavior()
    {
        if (playerTarget != null)
        {
            anim.SetTrigger("run");
            agent.speed = runSpeed;
            currentDestination = playerTarget.transform.position;
            agent.SetDestination(currentDestination);
        }
        else
        {
            playerSeen = false;
            currentState = State.Wandering;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        }
    }

    #region Vision
    void FindVisibleTargets()
    {
        playerTarget = null;
        playerSeen = false;

        if (player == null)
        {
            return;
        }


        Vector3 dirToTarget = (player.transform.position - transform.position).normalized;
        RaycastHit hit;
        if (Physics.Raycast(transform.position, dirToTarget, out hit))
        {
            float dstToTarget = Vector3.Distance(transform.position, player.transform.position);

            if (dstToTarget <= viewRadius)
            {
                if (Vector3.Angle(transform.forward, dirToTarget) <= viewAngle / 2)
                {
                    if (hit.collider.tag == "Player")
                    {
                        playerSeen = true;
                        playerTarget = hit.transform;
                    }
                }
            }
            
        }
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }

        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }


    // can we think of an alternative for this:
    // Maybe some random points around the outside the maze that get randomized and fed to the AI?
    /*
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        
        Vector3 randDirection = UnityEngine.Random.insideUnitSphere * dist;

        randDirection += origin;

        NavMeshHit navHit;

        // this is expensive
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }
    */
    #endregion

    // Update is called once per frame
    void Update () {
        CheckState();

        if (playerSeen)
        {
            currentState = State.Chasing;
        } else
        {
            currentState = State.Wandering;
        }
	}
}
