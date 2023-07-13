using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class Player : NetworkBehaviour {

    [SerializeField] private LayerMask cursorHitLayerMask;

    [SerializeField] private Projectile projectile;

    [SerializeField] private Transform hull;
    [SerializeField] private Transform turret;
    [SerializeField] private Transform cannon;
    [SerializeField] private Transform cannonEndPoint;

    private GameInput gameInput;


    private int health = 100;



    private void Awake() {
        SetColors();
    }
    private void SetColors() {
        Renderer _renderer = hull.gameObject.GetComponent<Renderer>();
        _renderer.material.color = Color.blue;

        _renderer = turret.gameObject.GetComponent<Renderer>();
        _renderer.material.color = Color.gray;

        _renderer = cannon.gameObject.GetComponent<Renderer>();
        _renderer.material.color = Color.black;
    }


    private void Start() {
        if(IsOwner == false) return;

        gameInput = GameInput.Instance;

        gameInput.OnShootPerformed += GameInput_OnShootPerformed_ServerRpc;
    }

    [ServerRpc]
    private void GameInput_OnShootPerformed_ServerRpc() {
        Projectile newProjectile = Instantiate(projectile);
        newProjectile.transform.position = cannonEndPoint.position;
        newProjectile.SetDirection(turret.forward);

        newProjectile.GetComponent<NetworkObject>().Spawn(true);
    }

    private void Update() {
        if(IsOwner == false) return;

        HandleMovement();
        HandleAiming();
    }



    private void HandleAiming() {
        Ray rayFromCamera = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Cannon look at cursor in the world
        Vector3 turretLookAtPoint;
        if(Physics.Raycast(rayFromCamera, out RaycastHit hit, cursorHitLayerMask)) {
            turretLookAtPoint = new Vector3(hit.point.x, turret.position.y, hit.point.z);
            turret.LookAt(turretLookAtPoint);
        }
    }

    private void HandleMovement() {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        float moveSpeed = 5f;
        float moveDistance = moveSpeed * Time.deltaTime;
        float playerRadius = 0.5f;
        float playerHight = 2f;
        bool canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHight, playerRadius, moveDir, moveDistance);

        if(!canMove) {
            // Cannot move towards moveDir

            // Attempt only X monevent
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHight, playerRadius, moveDirX, moveDistance);

            if(canMove) {
                // Can move only on the X
                moveDir = moveDirX;
            } else {
                // Can not move only on the X

                // Attempt only Z monevent
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHight, playerRadius, moveDirZ, moveDistance);

                if(canMove) {
                    // Can move only on the Z
                    moveDir = moveDirZ;
                } else {
                    // Can not move in any direction
                }
            }
        }

        if(canMove) {
            transform.position += moveDir * moveDistance;

            float rotateSpeed = 10f * Time.deltaTime;
            if(moveDir != Vector3.zero) {
                this.gameObject.transform.forward = Vector3.Slerp(hull.forward, moveDir, rotateSpeed);
            }
        }
    }


    public void Damage() {
        health -= 34;
        if(health < 0) {
            DestroySelf();
        }
    }
    private void DestroySelf() {
        Destroy(gameObject);

        gameInput.OnShootPerformed -= GameInput_OnShootPerformed_ServerRpc;
    }

}
