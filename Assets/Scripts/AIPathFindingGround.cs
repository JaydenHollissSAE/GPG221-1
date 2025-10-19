using UnityEngine;

public class AIPathFindingGround : AIPathFindingBase
{

    Rigidbody rb;

    public void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    private void Update()
    {
        FollowPath(); // Done in update rather than FixedUpdate to make movement more smooth
    }

    public void FollowPath()
    {
        if (!awaitCalculation && !AIGrid.instance.disableMove) // Checks if the logic is allowing the character to move
        {
            if (pathCellPositions.Count > 0) // Ensures a path exists to follow
            {
                // Gets the closest path position to the character and removes one ones before that to prevent the following trying to go backwards
                int closestIndex = 0;
                float closestDistance = 10000f;
                for (int i = 0; i < pathCellPositions.Count; i++)
                {
                    Vector3 tmpPos = pathCellPositions[i];
                    tmpPos.y = characterY;
                    pathCellPositions[i] = tmpPos;
                    float distance = Vector3.Distance(transform.position, pathCellPositions[i]);
                    if (distance < closestDistance)
                    {
                        closestIndex = i;
                        closestDistance = distance;
                    }
                }
                if (closestIndex > 0) pathCellPositions.RemoveRange(0, closestIndex);



                //Debug.Log("Move");


                // Looks towards the desired path position and moves towards it

                Vector3 lookPos;

                if (pathCellPositions[0].y < characterY)
                {
                    lookPos = new Vector3(pathCellPositions[0].x, transform.forward.y, pathCellPositions[0].z);
                }
                else lookPos = pathCellPositions[0];

                //lookPos.y = transform.position.y; // This fixes a few bugs reguarding to looking direction, but I like the bobbing heads too much to use the fix :>

                transform.LookAt(lookPos, Vector3.up);

                // I love this but it breaks when going down // I don't remember what this comment meant but I'll leave it because why not
                rb.linearVelocity = transform.forward * 10f;
                if (pathCellPositions.Count > 1) pathCellPositions[0] = CheckPathClarity(pathCellPositions[0], pathCellPositions[1]);

                // If the character is close enough to the position that position is removed from the path finding list
                if (Vector3.Distance(transform.position, pathCellPositions[0]) < 0.8f)
                {
                    pathCellPositions.RemoveAt(0);
                }

            }
            else StartCoroutine(CalculatePath()); // Calculates the path if the path is empty

        }

    }

    Vector3 CheckPathClarity(Vector3 inputPos, Vector3 nextInputPos)
    {
        // Checks if anything is in the way of the character
        Vector3 outputPos = inputPos;
        if (inputPos.y < characterY || AIGrid.instance.grid[(int)inputPos.x, (int)inputPos.y, (int)inputPos.z].state == "stairs") return inputPos; // Returns without doing anything if the target position is below the character or the target cell is stairs
        else
        {
            RaycastHit hit;
            bool hitDetction = Physics.BoxCast(new Vector3(inputPos.x, inputPos.y, inputPos.z), AIGrid.instance.scaledCellSize, Vector3.zero, out hit); // Checks if anything exists in the target cell
            if (hitDetction)
            {
                // Checks next to the cell on either the X or Z value depending on the next cell position
                for (int i = -1; i <= 1; i += 2)
                {
                    Vector3 checkPos;
                    if (nextInputPos.x != inputPos.x)
                    {
                        checkPos = new Vector3(inputPos.x + (AIGrid.instance.scaledCellSize.x * i), inputPos.y, inputPos.z);
                    }
                    else
                    {
                        checkPos = new Vector3(inputPos.x, inputPos.y, inputPos.z + (AIGrid.instance.scaledCellSize.z * i));
                    }

                    // Checks if something exists in the cell next to it
                    RaycastHit hit2;
                    bool hitDetction2 = Physics.BoxCast(checkPos, AIGrid.instance.scaledCellSize, Vector3.zero, out hit2);
                    if (!hitDetction2) // If nothing hit, checks if the cell is able to be traversed, sets new target if able to be
                    {
                        string state = AIGrid.instance.grid[(int)checkPos.x, (int)checkPos.y, (int)checkPos.z].state;
                        if (state == "stairs" || state == "walkable")
                        {
                            outputPos = checkPos;
                            break;
                        }
                    }

                }
            }

        }
        return outputPos;

    }


    private void OnCollisionEnter(Collision collision)
    {
        ColliderJump(collision);
    }
    private void OnCollisionStay(Collision collision)
    {
        ColliderJump(collision);
    }

    private void ColliderJump(Collision collision)
    {
        // Makes character jump up stairs
        if (collision.transform.position.y >= characterY)
        {
            Vector3 collidedWith = transform.position;
            Vector3 adjustBy = AIGrid.instance.scaledCellSize;
            if (collision.contacts[0].point.x < transform.position.x) adjustBy.x *= -1;
            adjustBy.y = 0f;
            if (collision.contacts[0].point.z < transform.position.z) adjustBy.z *= -1;
            collidedWith += adjustBy;

            clearJumpVisualisation.Invoke();

            jumpPos = new Vector3(Mathf.FloorToInt(collidedWith.x), Mathf.FloorToInt(collidedWith.y), Mathf.FloorToInt(collidedWith.z));

            VisualisationSetter.instance.SpawnVisualisation(jumpPos, AIGrid.instance.scaledCellSize, "jump", gameObject);


            string state = AIGrid.instance.grid[Mathf.FloorToInt(collidedWith.x), Mathf.FloorToInt(collidedWith.y), Mathf.FloorToInt(collidedWith.z)].state;
            //Debug.Log(state);
            if (state == "stairs")
            {
                rb.AddForce(Vector3.up * 10f, ForceMode.VelocityChange);
                rb.linearVelocity = transform.forward * 10f;
            }
            else if (state == "unwalkable" || state == "air") // Stops being stuck in the air
            {
                //Debug.Log("Push Down");
                rb.AddForce(Vector3.down * 4f, ForceMode.VelocityChange);

            }
        }

    }

    Vector3 jumpPos = Vector3.zero;



}
