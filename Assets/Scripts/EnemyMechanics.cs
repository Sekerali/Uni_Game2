using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class EnemyMechanics : MonoBehaviour
{

    [SerializeField] private Text PlayerHealth;
    [SerializeField] private AudioClip fire;
    [SerializeField] private AudioClip hurt;
    [SerializeField] private AudioClip playerhurt;


    NavMeshAgent agent;
    Transform player;
    Transform enemies;
    Vector3 enemyposition;
    Vector3 movetoposition;
    Vector3 lookatposition;
    Vector3 lastseenposition;
    Animation anim;
    Animation gun;

    int Health = 40;
    int VCloseRange = 3;
    int CloseRange = 6;
    int ViewRange = 30;
    int Fov = 80;
    float TurnSpeed = 1;
    float TurnAngle = 50;
    int Fear = 0;

    int lastseentype = 1;
    int searchcount = 1;
    bool playerseen = false;
    bool cover = false;
    bool coverout = false;
    bool coverplayerseen = false;
    bool forward;
    int behaviour = 0;
    int prevb;
    Vector3 odir;
    Vector3 dir;
    int[] points = new int[3];
    float currtime = 0;
    float waittime = -1;
    bool ignorenearbycover = false;
    float shootcurr = 0f;
    float updatetime = 0f;

    int node = -1;
    int nodetype = 0; //0 = idle, 1 = cover, 2 = wall
    List<int> blacklistnode = new List<int>();

    List<int> teamhp = new List<int>();
    List<int> ignoreai = new List<int>();


    void Start()
    {
        agent = transform.parent.GetComponentInChildren<NavMeshAgent>();
        anim = transform.parent.GetComponentInChildren<Animation>();
        gun = transform.parent.GetChild(1).GetComponentInChildren<Animation>();
        Fov = Fov / 2;
        player = GameObject.Find("FPSController").transform;
        enemies = GameObject.Find("Enemies").transform;

        for (int i = 0; i < enemies.childCount; i++)
        {
            teamhp.Add(40);
            if (transform.parent == enemies.GetChild(i))
            {
                ignoreai.Add(i);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(lookatposition, new Vector3(1, 1, 1));
        Gizmos.color = Color.green;
        Gizmos.DrawCube(lastseenposition, new Vector3(1, 1, 1));
        Gizmos.color = Color.red;
        Gizmos.DrawCube(movetoposition, new Vector3(1, 1, 1));
        if (agent != null)
        {
            if (agent.path.corners.Length > 2)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawCube(agent.path.corners[1], new Vector3(1, 1, 1));
            }
        }
        Gizmos.color = Color.white;
        Gizmos.DrawCube(enemyposition, new Vector3(1, 1, 1));


    }

    void Update()
    {
        enemyposition = transform.parent.position - new Vector3(0, 0.9f, 0); //get feet position for pathing
        transform.parent.GetChild(1).position = transform.GetChild(0).GetChild(0).position; //place gun on hand
        transform.parent.GetChild(1).eulerAngles = transform.GetChild(0).eulerAngles; //rotate gun
        if (!anim.IsPlaying("EnemyCrouch") && !anim.IsPlaying("EnemyDead"))
        {
            anim.Stop();
        }
        if (!gun.IsPlaying("GunFire"))
        {
            gun.Stop();
        }

        //health
        if (Health <= 0)
        {
            if (behaviour != -1)
            {
                anim.Play("EnemyDead");
                behaviour = -1;
                nodetype = -1;
                node = -1;
                agent.destination = transform.position;
                transform.parent.forward = transform.GetChild(0).forward;
                Destroy(transform.GetComponent<Rigidbody>());
                Destroy(transform.GetComponent<CapsuleCollider>());
            }
            return;
        }

        /*
        behaviour = -1 -> dead
        behaviour = 0 -> patrol (default)
        behaviour = 1 -> look for last seen
        behaviour = 2 -> patrol open areas
        bahaviour = 3 -> alarmed, conditions can vary
        behaviour = 4 -> standard combat
        behaviour = 5 -> increased aggression while winning
        behaviour = 6 -> increased defensive while losing
        behaviour = 7 -> massive increase aggression while losing
        behaviour = 8 -> massive increase defensive while losing
        behaviour = 9 -> waiting - looking around while standing
        */

        //check player sighting
        playerseen = false;
        if (EnemyPathing.LookForPlayer(transform, player, Fov, ViewRange, VCloseRange, CloseRange) == true)
        {
            playerseen = true;
            lastseenposition = player.position;
            lastseentype = 1;
        }

        //gunfire
        if (playerseen == true)
        {
            if (shootcurr > 1f)
            {
                shootcurr = 0f;
                //transform.parent.forward = transform.GetChild(0).forward;
                //gun.Play("GunFire");
                AudioSource.PlayClipAtPoint(fire, transform.GetChild(0).GetChild(0).position);
                RaycastHit shot;
                if (Physics.Raycast(transform.GetChild(1).position, transform.GetChild(0).forward, out shot))
                {
                    if (shot.transform == player)
                    {
                        AudioSource.PlayClipAtPoint(playerhurt, player.position);
                        Fear -= 5;
                        string a = PlayerHealth.text.Substring(7);
                        PlayerHealth.text = PlayerHealth.text.Substring(0, 8) + (int.Parse(PlayerHealth.text.Substring(7)) - 5).ToString();
                    }
                }
            }
            else
            {
                shootcurr += Time.deltaTime;
            }
        }

        //update lastseen
        if (((behaviour > 3) || (behaviour == 1)) && (behaviour != 9))
        {
            while (lastseentype < 3)
            {
                if (EnemyPathing.PredictPlayer(player, lastseenposition, lastseentype) == true)
                {
                    playerseen = true;
                    break;
                }
                else
                {
                    lastseenposition = player.position;
                    lastseentype++;
                }
            }
        }

        //check nearby enemies
        for (int i = 0; i < enemies.childCount; i++)
        {
            if (!ignoreai.Contains(i)) //dont check itself or already seen dead
            {
                Vector3 dir = enemies.GetChild(i).position - transform.position; //nearby parameters
                if (((Vector3.Distance(transform.position, enemies.GetChild(i).position) < ViewRange) && (Vector3.Angle(transform.forward, dir) < Fov)) || (Vector3.Distance(transform.position, enemies.GetChild(i).position) < CloseRange))
                {
                    RaycastHit hit;
                    if (Physics.Linecast(transform.position, enemies.GetChild(i).position, out hit, (1 << 0) | (1 << 12)))
                    {
                        if (hit.collider.name == "Body")
                        {
                            Debug.DrawLine(transform.position, enemies.GetChild(i).position, Color.magenta, 0.1f);

                            //get nearby ai variables
                            int nbehaviour = hit.transform.GetComponent<EnemyMechanics>().behaviour;
                            int nnodetype = hit.transform.GetComponent<EnemyMechanics>().nodetype;
                            int nnode = hit.transform.GetComponent<EnemyMechanics>().node;
                            float ncurrtime = hit.transform.GetComponent<EnemyMechanics>().currtime;
                            float nwaittime = hit.transform.GetComponent<EnemyMechanics>().waittime;
                            bool ncover = hit.transform.GetComponent<EnemyMechanics>().cover;
                            bool ncoverout = hit.transform.GetComponent<EnemyMechanics>().coverout;
                            bool nignorenearbycover = hit.transform.GetComponent<EnemyMechanics>().ignorenearbycover;
                            Vector3 nlastseenposition = hit.transform.GetComponent<EnemyMechanics>().lastseenposition;
                            Vector3 nmovetoposition = hit.transform.GetComponent<EnemyMechanics>().movetoposition;

                            //fix wait behaviour
                            int tempb = behaviour;
                            if (behaviour == 9)
                            {
                                tempb = prevb;
                            }
                            if (nbehaviour == 9)
                            {
                                nbehaviour = hit.transform.GetComponent<EnemyMechanics>().prevb;
                            }

                            if ((tempb == 0) && (nbehaviour != 0)) //check if nearby alarmed or dead when patrolling
                            {
                                if (nbehaviour == -1)
                                {
                                    lastseenposition = hit.transform.position;
                                    behaviour = 2;
                                    ignoreai.Add(i);
                                    Wait(15, 180);
                                }
                                else if (tempb > 3)
                                {
                                    behaviour = 3;
                                    movetoposition = nlastseenposition;
                                }
                                else
                                {
                                    behaviour = 3;
                                    movetoposition = nmovetoposition;
                                }
                            }
                            else if (nbehaviour == -1) //if nearby dead while alarmed, add to ignore list
                            {
                                ignoreai.Add(i);
                            }
                            else if ((tempb < 3) && (nbehaviour > 2)) { //check if nearby alarmed knows player position
                                behaviour = 3;
                                movetoposition = nlastseenposition;
                            }
                            else if ((nnodetype == nodetype) && (nnode == node) && (node != -1)) //dont move to same node
                            {
                                Vector3 dest = EnemyPathing.GetNodePosition(nodetype, node);
                                if (EnemyPathing.Distance(hit.transform.position, dest) < EnemyPathing.Distance(transform.position, dest))
                                {
                                    blacklistnode.Add(node);
                                    node = -1;
                                    nodetype = -1;
                                }
                            }
                            else if ((ignorenearbycover == false) && (nignorenearbycover == false) && (cover == true) && (ncover == true))  //in and out of cover same time
                            {
                                if ((coverout == true) && (ncoverout == false))
                                {
                                    currtime = waittime;
                                }
                                else if (waittime > nwaittime)
                                {
                                    waittime = nwaittime;
                                    currtime = ncurrtime;
                                }
                            }

                            //update fear
                            Fear = EnemyPathing.UpdateFear(i, teamhp[i], hit.transform.GetComponent<EnemyMechanics>().Health, Fear);
                            teamhp[i] = hit.transform.GetComponent<EnemyMechanics>().Health;

                        }
                    }
                }
            }
        }

        if ((playerseen == true) && (behaviour == 4))
        {
            if (Fear >= 30)
            {
                if (Random.Range(1, 4) == 1)
                {
                    behaviour = 7;
                }
                else
                {
                    behaviour = 8;
                }
            }
            else if (Fear >= 15)
            {
                behaviour = 6;
            }
            else if (Fear <= -20)
            {
                behaviour = 5;
            }
        }

        //behaviours
        if (behaviour == 0)
        {
            agent.stoppingDistance = 2f;

            if (blacklistnode.Count == EnemyPathing.PNode.Count)
            {
                blacklistnode.Clear();
            }

            if (playerseen == true)
            {
                behaviour = 4;
            }
            else if (node == -1)
            {
                nodetype = 0;
                node = EnemyPathing.StandardPathing(blacklistnode);
                movetoposition = EnemyPathing.PNode[node];
            }
            else if (Vector3.Distance(enemyposition, movetoposition) < agent.stoppingDistance)
            {
                blacklistnode.Clear();
                node = EnemyPathing.StandardPathing(blacklistnode);
                movetoposition = EnemyPathing.PNode[node];
                Wait(45, 120);
            }
        }
        else if (behaviour == 1)
        {
            nodetype = -1;
            node = -1;
            agent.stoppingDistance = 4f;
            movetoposition = lastseenposition;

            if (playerseen == true)
            {
                behaviour = 4;
            }
            else if (Vector3.Distance(enemyposition, movetoposition) < agent.stoppingDistance)
            {
                behaviour = 2;
                Wait(5, 180);
            }
        }
        else if (behaviour == 2)
        {
            nodetype = -1;
            agent.stoppingDistance = 6f;
            if (playerseen == true)
            {
                behaviour = 4;
            }
            else if (Vector3.Distance(enemyposition, movetoposition) < agent.stoppingDistance)
            {
                if (searchcount < 4)
                {
                    if (searchcount == 1)
                    {
                        points = EnemyPathing.SearchPlayerPathing(lastseenposition);
                    }
                    movetoposition = EnemyPathing.INode[points[searchcount - 1]];
                    Wait(8, 360);
                    searchcount++;
                }
                else
                {
                    behaviour = 0;
                    node = -1;
                    nodetype = 0;
                    Wait(8, 120);
                }
            }
        }
        else if (behaviour == 3)
        {
            node = -1;
            nodetype = -1;
            agent.stoppingDistance = 6f;
            if (playerseen == true)
            {
                behaviour = 4;
            }
            else if (Vector3.Distance(enemyposition, movetoposition) < agent.stoppingDistance)
            {
                Wait(10, 120);
                behaviour = 2;
            }
        }
        else if (behaviour == 4)
        {
            agent.stoppingDistance = 0.5f;
            Cover();
        }
        else if (behaviour == 5)
        {
            agent.stoppingDistance = 0.5f;
            ignorenearbycover = true;
            Cover();
        }
        else if (behaviour == 6)
        {
            agent.stoppingDistance = 0.5f;
            ignorenearbycover = true;
            Cover();
        }
        else if (behaviour == 7)
        {
            agent.stoppingDistance = 5f;
            node = -1;
            nodetype = -1;
            movetoposition = lastseenposition;
            if (playerseen == false)
            {
                behaviour = 2;
            }
        }
        else if (behaviour == 8)
        {
            agent.stoppingDistance = 0.5f;
            node = -1;
            nodetype = -1;
            movetoposition = (lastseenposition - enemyposition) * -3f;
            if (playerseen == false)
            {
                behaviour = 1;
            }
        }
        else if (behaviour == 9)
        {
            if (playerseen == true)
            {
                behaviour = 4;
                currtime = 0;
            }
            else if (currtime < waittime)
            {
                dir = EnemyPathing.Waiting(transform, TurnAngle, TurnSpeed * 5, dir, odir);
                forward = true;
                currtime += Time.deltaTime;
            }
            else
            {
                behaviour = prevb;
                currtime = 0;
            }
        }

        if ((behaviour < 4) || (behaviour > 6))
        {
            cover = false;
        }

        if (behaviour != 9)
        {
            EnemyPathing.LookPosition(transform, behaviour, lastseenposition, movetoposition, agent, cover, coverout, node, nodetype, out lookatposition, out forward);
            EnemyPathing.RotateEnemy(transform, lookatposition, TurnSpeed, forward);
            agent.SetDestination(movetoposition);
        }
    }

    void Wait(float time, float angle)
    {
        prevb = behaviour;
        behaviour = 9;
        waittime = time;
        TurnAngle = angle / 2;
        odir = transform.forward;
        dir = transform.forward;
    }

    void Crouching(bool crouch)
    {
        if (crouch == true)
        {
            if (anim.IsPlaying("EnemyCrouch"))
            {
                anim["EnemyCrouch"].speed = 1f;
            }
            else
            {
                anim["EnemyCrouch"].time = 0f;
                anim["EnemyCrouch"].speed = 1f;
                anim.Play("EnemyCrouch");
            }
        }
        else
        {
            if (anim.IsPlaying("EnemyCrouch"))
            {
                anim["EnemyCrouch"].speed = -1f;
            }
            else
            {
                anim["EnemyCrouch"].time = 0.5f;
                anim["EnemyCrouch"].speed = -1f;
                anim.Play("EnemyCrouch");
            }
        }
    }

    void Cover()
    {
        if (nodetype == -1)
        {
            EnemyPathing.TakeCover(player, transform, lastseenposition, blacklistnode, ViewRange, out nodetype, out node);
        }

        if (Vector3.Distance(enemyposition, movetoposition) < agent.stoppingDistance)
        {
            if ((waittime == -1) && (nodetype == 1))
            {
                Crouching(true);
            }

            if (currtime == 0)
            {
                float scale = 1f;
                if (behaviour == 4)
                {
                    scale = 1f;
                }
                else if (behaviour == 5)
                {
                    if (coverout == true)
                    {
                        scale = 2f;
                    }
                    else
                    {
                        scale = 0.5f;
                    }
                }
                else if (behaviour == 6)
                {
                    if (coverout == true)
                    {
                        scale = 0.5f;
                    }
                    else
                    {
                        scale = 2f;
                    }
                }
                waittime = Random.Range(4, 8) * scale;
            }
            cover = true;
            blacklistnode.Clear();
        }

        if (cover == true)
        {
            if (playerseen == true)
            {
                if (EnemyPathing.CoverOK(player, node, nodetype) == false)
                {
                    if (nodetype == 1)
                    {
                        Crouching(false);
                    }
                    EnemyPathing.TakeCover(player, transform, lastseenposition, blacklistnode, ViewRange, out nodetype, out node);
                    if (nodetype == 1)
                    {
                        movetoposition = EnemyPathing.CNode[node];
                    }
                    else if (nodetype == 2)
                    {
                        movetoposition = EnemyPathing.WNode[node];
                    }
                    else
                    {
                        agent.stoppingDistance = 5f;
                        movetoposition = lastseenposition;
                    }
                    waittime = -1f;
                    currtime = 0f;
                    cover = false;
                    coverplayerseen = false;
                    coverout = false;
                }
                else
                {
                    coverplayerseen = true;
                }
            }

            if (currtime < waittime)
            {
                currtime += Time.deltaTime;
            }
            else if (waittime != -1)
            {
                currtime = 0;
                if (coverout == true)
                {
                    if (coverplayerseen == false)
                    {
                        if (nodetype == 1)
                        {
                            Crouching(false);
                        }
                        node = -1;
                        nodetype = -1;

                        if (behaviour == 4)
                        {
                            behaviour = 1;
                            Wait(5, 60);
                        }
                        else if (behaviour == 5)
                        {
                            behaviour = 1;
                        }
                        else if (behaviour == 6)
                        {
                            behaviour = 1;
                            Wait(10, 60);
                        }
                        
                    }
                    else
                    {
                        coverout = false;
                        if (nodetype == 1)
                        {
                            Crouching(true);
                        }
                        else
                        {
                            movetoposition = EnemyPathing.WallNode.GetChild(node).position;
                        }
                    }
                }
                else
                {
                    if (behaviour == 4)
                    {
                        ignorenearbycover = false;
                    }
                    coverplayerseen = false;
                    coverout = true;
                    if (nodetype == 1)
                    {
                        Crouching(false);
                    }
                    else
                    {
                        movetoposition = EnemyPathing.WallNode.GetChild(node).GetChild(1).position;
                    }
                }
            }
        }
        else
        {
            if (updatetime < 0.3f)
            {
                updatetime += Time.deltaTime;
            }
            else
            {
                updatetime = 0f;
                EnemyPathing.TakeCover(player, transform, lastseenposition, blacklistnode, ViewRange, out nodetype, out node);
                if (nodetype == 1)
                {
                    movetoposition = EnemyPathing.CNode[node];
                }
                else if (nodetype == 2)
                {
                    movetoposition = EnemyPathing.WNode[node];
                }
                else
                {
                    agent.stoppingDistance = 5f;
                    movetoposition = lastseenposition;
                }
            }
        }   
    }

    void Damage(int damage)
    {
        Health -= damage;
        Fear += 5;
        AudioSource.PlayClipAtPoint(hurt, transform.position);
        if ((behaviour < 4) || (behaviour == 9)) {
            currtime = 0;
            behaviour = 1;
            lastseenposition = player.position;
        }
        else if (cover == true)
        {
            if (coverout == true)
            {
                ignorenearbycover = true;
                currtime = waittime;
            }
        }

    }
}