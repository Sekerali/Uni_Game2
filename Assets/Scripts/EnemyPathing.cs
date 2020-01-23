using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public static class EnemyPathing
{
    public static Transform PathNode;
    public static Transform InvestigateNode;
    public static Transform CoverNode;
    public static Transform WallNode;

    public static List<Vector3> PNode = new List<Vector3>();
    public static List<Vector3> INode = new List<Vector3>();
    public static List<Vector3> CNode = new List<Vector3>();
    public static List<Vector3> WNode = new List<Vector3>();

    static EnemyPathing()
    {

        PathNode = GameObject.Find("Node_P").transform;
        InvestigateNode = GameObject.Find("Node_I").transform;
        CoverNode = GameObject.Find("Node_C").transform;
        WallNode = GameObject.Find("Node_W").transform;

        //get path nodes
        int i = 0;
        foreach (Transform node in PathNode)
        {
            i++;
            PNode.Add(node.position);
        }

        //get investigate nodes
        i = 0;
        foreach (Transform node in InvestigateNode)
        {
            i++;
            INode.Add(node.position);
        }

        //get cover nodes
        i = 0;
        foreach (Transform node in CoverNode)
        {
            i++;
            CNode.Add(node.position);
        }

        //get investigate nodes
        i = 0;
        foreach (Transform node in WallNode)
        {
            i++;
            WNode.Add(node.position);
        }

    }

    public static int StandardPathing(List<int> blacklist)
    {
        int node;

        if (blacklist.Count == 0)
        {
            node = Random.Range(0, PNode.Count);
        }
        else
        {
            List<int> tempnode = new List<int>();
            for (int i = 0; i < PNode.Count; i++)
            {
                tempnode.Add(i);
            }

            for (int i = 0; i < blacklist.Count; i++)
            {
                if (tempnode.Contains(blacklist[i]))
                {
                    tempnode.Remove(blacklist[i]);
                }
            }

            node = Random.Range(0, tempnode.Count);
        }
        

        return node;
    }

    public static int[] SearchPlayerPathing(Vector3 lastseen)
    {
        int[] points = new[] { -1, -1, -1 };
        double[] distance = new[] { -1.0, -1.0, -1.0 };
        double tempdistance;

        for (int i = 0; i < INode.Count; i++)
        {
            tempdistance = Distance(lastseen, INode[i]);
            if (tempdistance == 0)
            {
                tempdistance = Vector3.Distance(lastseen, INode[i]);
            }

            if ((tempdistance < distance[0]) || (distance[0] == -1))
            {
                distance[2] = distance[1];
                distance[1] = distance[0];
                distance[0] = tempdistance;
                points[2] = points[1];
                points[1] = points[0];
                points[0] = i;
            }
            else if ((tempdistance < distance[1]) || (distance[1] == -1))
            {
                distance[2] = distance[1];
                distance[1] = tempdistance;
                points[2] = points[1];
                points[1] = i;
            }
            else if ((tempdistance < distance[2]) || (distance[2] == -1))
            {
                distance[2] = tempdistance;
                points[2] = i;
            }
        }

        return points;
    }

    public static void TakeCover(Transform player, Transform enemy, Vector3 lastseen, List<int> blacklist, int ViewRange, out int return1, out int return2)
    {
        List<int> ctemp = new List<int>();
        List<int> wtemp = new List<int>();

        //check possible covers
        for (int i = 0; i < CNode.Count; i++)
        {

            Vector3 location = CoverNode.GetChild(i).GetChild(1).position;
            Vector3 direction = lastseen - location;
            Vector3 forward = CoverNode.GetChild(i).GetChild(0).position - location;

            if (Vector3.Angle(forward, direction) < 60) //if within view angle
            {
                if (!Physics.Linecast(location, lastseen, 1 << 0)) //no obstruction
                {
                    double enemydist = Distance(enemy.position, location);
                    double playerdist = Distance(lastseen, location);
                    if (playerdist == 0) //possible player out of bounds
                    {
                        enemydist = Vector3.Distance(enemy.position, location);
                        playerdist = Vector3.Distance(lastseen, location);
                    }

                    if ((enemydist < playerdist) && (enemydist < 40))
                    {
                        ctemp.Add(i);
                        Debug.DrawLine(lastseen, location, Color.cyan, 0.1f);
                    }
                }
            }
        }

        //check possible walls
        for (int i = 0; i < WNode.Count; i++)
        {
            Vector3 location = WallNode.GetChild(i).GetChild(1).position;
            Vector3 direction = lastseen - location;
            Vector3 forward = WallNode.GetChild(i).GetChild(0).position - location;
            float length = Vector3.Distance(location, player.position);

            if (Vector3.Angle(forward, direction) < 60) //if within view angle
            {
                if (!Physics.Linecast(location, lastseen, 1 << 0)) //no obstruction
                {
                    double enemydist = Distance(enemy.position, location);
                    double playerdist = Distance(lastseen, location);
                    if (playerdist == 0) //possible player out of bounds
                    {
                        enemydist = Vector3.Distance(enemy.position, location);
                        playerdist = Vector3.Distance(lastseen, location);
                    }

                    if ((enemydist < playerdist) && (enemydist < 40))
                    {
                        wtemp.Add(i);
                        Debug.DrawLine(lastseen, location, Color.cyan, 0.1f);
                    }
                }
            }
        }

        //remove used nodes
        for (int i = 0; i < blacklist.Count; i++)
        {
            if (ctemp.Contains(blacklist[i]))
            {
                ctemp.Remove(blacklist[i]);
            }
        }

        for (int i = 0; i < blacklist.Count; i++)
        {
            if (wtemp.Contains(blacklist[i]))
            {
                wtemp.Remove(blacklist[i]);
            }
        }

        //if no cover found
        if ((wtemp.Count == 0) && (ctemp.Count == 0))
        {
            return1 = -1;
            return2 = -1;
            return;
        }

        //get shortest distance
        int node = -1;
        bool wall = false;
        double distance = -1;
        double tempdistance;
        for (int i = 0; i < ctemp.Count; i++)
        {
            tempdistance = Distance(enemy.position, CNode[ctemp[i]]);

            if ((tempdistance < distance) || (distance == -1))
            {
                distance = tempdistance;
                node = ctemp[i];
                wall = false;
            }
        }

        for (int i = 0; i < wtemp.Count; i++)
        {
            tempdistance = Distance(enemy.position, WNode[wtemp[i]]);

            if ((tempdistance < distance) || (distance == -1))
            {
                distance = tempdistance;
                node = wtemp[i];
                wall = true;
            }
        }

        if (wall == false)
        {
            return1 = 1;
            return2 = node;
            return;
        }
        else
        {
            return1 = 2;
            return2 = node;
            return;
        }


    }

    public static bool LookForPlayer(Transform enemy, Transform player, int Fov, int ViewRange, int VCloseRange, int CloseRange)
    {
        Vector3 location = enemy.GetChild(1).position;
        Vector3 direction = player.position - enemy.position;
        RaycastHit hit;

        //sight pathing
        if (Vector3.Angle(enemy.forward, direction) < Fov) //if within view angle
        {
            if (Physics.Raycast(location, direction, out hit, ViewRange)) //if no obstruction and within range
            {
                Debug.DrawLine(location, hit.point, Color.white, 0.1f);
                if (hit.transform == player)
                {
                    return true;
                }
            }
        }

        //radius pathing
        if (Vector3.Distance(location, player.position) < VCloseRange) //through walls
        {
            Debug.DrawLine(location, player.position, Color.red, 0.1f);
            return true;
        }

        hit = new RaycastHit();
        if (Physics.Raycast(location, direction, out hit, CloseRange)) //not through walls
        {
            Debug.DrawLine(location, hit.point, Color.blue, 0.1f);
            if (hit.transform == player)
            {
                return true;
            }
        }

        return false;
    }

    public static bool PredictPlayer(Transform player, Vector3 lastseen, int lastseentype)
    {
        if (Vector3.Distance(lastseen, player.position) < 5) //through walls
        {
            Debug.DrawLine(lastseen, player.position, Color.green, 0.1f);
            return true;
        }

        if (lastseentype == 2)
        {
            RaycastHit hit;
            Vector3 direction = player.position - lastseen;
            if (Physics.Raycast(lastseen, direction, out hit, 10) || (lastseen == player.position)) //not through walls
            {
                Debug.DrawLine(lastseen, hit.point, Color.yellow, 0.1f);
                if ((hit.transform == player) || (lastseen == player.position))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static void RotateEnemy(Transform enemy, Vector3 destination, float TurnSpeed, bool forward)
    {
        Vector3 direction = destination - enemy.position;
        GameObject arm = enemy.GetChild(0).gameObject;
        Vector3 direction_arm = destination - arm.transform.position;
        Quaternion lookArm = Quaternion.LookRotation(direction_arm);
        Quaternion lookBody = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        if (forward)
        {
            lookArm = Quaternion.LookRotation(new Vector3(direction_arm.x, 0, direction_arm.z));
        }
        enemy.rotation = Quaternion.Slerp(enemy.rotation, lookBody, Time.deltaTime * TurnSpeed * 3f);
        arm.transform.rotation = Quaternion.Slerp(arm.transform.rotation, lookArm, Time.deltaTime * TurnSpeed * 3f);
    }

    public static Vector3 Waiting(Transform enemy, float angle, float TurnSpeed, Vector3 dir, Vector3 original)
    {
        if (Vector3.Angle(dir, enemy.forward) < 1)
        {
            float a = Random.Range(20, angle + 1);
            if (Random.Range(1, 3) == 2)
            {
                a = -a;
            }

            dir = Quaternion.AngleAxis(a, Vector3.up) * original;
        }
        Quaternion lookBody = Quaternion.LookRotation(dir);
        enemy.rotation = Quaternion.Slerp(enemy.rotation, lookBody, Time.deltaTime * TurnSpeed / 5);

        return dir;
    }

    public static bool CoverOK(Transform player, int node, int nodetype)
    {
        Vector3 direction;
        Vector3 forward;
        if (nodetype == 1)
        {
            direction = (player.position - CoverNode.GetChild(node).GetChild(1).position);
            forward = (CoverNode.GetChild(node).GetChild(0).position - CoverNode.GetChild(node).GetChild(1).position);
            float a = Vector3.Angle(direction, forward);
            if (Vector3.Angle(direction, forward) > 60)
            {
                return false;
            }
        }
        else if (nodetype == 2)
        {
            direction = (player.position - WallNode.GetChild(node).GetChild(1).position);
            forward = (WallNode.GetChild(node).GetChild(0).position - WallNode.GetChild(node).GetChild(1).position);
            if (Vector3.Angle(direction, forward) > 60)
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        return true;
    }

    public static void LookPosition(Transform enemy, int behaviour, Vector3 lastseen, Vector3 movetoposition, NavMeshAgent agent, bool cover, bool coverout, int node, int nodetype, out Vector3 return1, out bool return2)
    {
        if (!Physics.Linecast(enemy.position, lastseen, 1 << 0))
        {
            return1 = lastseen;
            return2 = false;
            return;
        }
        else if (cover == true)
        {
            if (coverout == true)
            {
                return1 = lastseen;
                return2 = false;
                return;
            }
            else
            {
                if (nodetype == 1)
                {
                    return1 = CoverNode.GetChild(node).GetChild(1).position;
                }
                else
                {
                    return1 = WallNode.GetChild(node).GetChild(1).position;
                }
                return2 = false;
                return;
            }
        }
        else
        {
            if (agent.path.corners.Length > 2)
            {
                return1 = agent.path.corners[1];
                return2 = true;
                return;
            }
            else
            {
                return1 = movetoposition;
                return2 = true;
                return;
            }
        }
    }

    public static double Distance(Vector3 start, Vector3 destination)
    {
        double distance = 0;
        NavMeshPath path = new NavMeshPath();
        NavMesh.CalculatePath(start, destination, NavMesh.AllAreas, path);

        for (int i = 1; i < path.corners.Length; i++)
        {
            distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return distance;
    }

    public static Vector3 GetNodePosition(int nodetype, int node)
    {
        if (nodetype == 0)
        {
            return PNode[node];
        }
        else if (nodetype == 1)
        {
            return CNode[node];
        }
        else if (nodetype == 2)
        {
            return WNode[node];
        }
        else
        {
            return Vector3.zero;
        }
    }

    public static int UpdateFear(int id, int oldhp, int newhp, int fear)
    {
        if (oldhp == newhp)
        {
            return fear;
        }
        else
        {
            fear += 1;

            if ((oldhp > 20) && (newhp <= 20)) {
                fear += 5;
            }

            if ((oldhp > 0) && (newhp <= 0))
            {
                fear += 10;
            }
        }

        return fear;
    }
}