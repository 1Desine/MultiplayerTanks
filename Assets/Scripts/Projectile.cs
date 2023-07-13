using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Projectile : NetworkBehaviour {


    Vector3 moveDir;
    float lifeTime = 5f;
    float flySpeed = 10f;

    private void Update() {
        Move();

        lifeTime -= Time.deltaTime;
        if(lifeTime < 0) {
            DestroySelf_ServerRpc();
        }

        float rayDistance = 0.35f / 2; // Projectile radius
        if(Physics.Raycast(transform.position, moveDir, out RaycastHit hit, rayDistance)) {
            if(hit.collider.gameObject.TryGetComponent(out Player player)) {
                player.Damage();
                Destroy(this.gameObject);
            } else if(hit.collider.gameObject.TryGetComponent(out Projectile projectile)) {
                projectile.DestroySelf_ServerRpc();
                DestroySelf_ServerRpc();
            } else {
                moveDir = Vector3.Reflect(moveDir, hit.normal);
                flySpeed *= 0.6f;
            }
        }
    }



    private void Move() {
        transform.position += moveDir * flySpeed * Time.deltaTime;
    }

    public void SetDirection(Vector3 moveDirection) {
        this.moveDir = moveDirection;
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroySelf_ServerRpc() {
        Destroy(this.gameObject);
    }


}
