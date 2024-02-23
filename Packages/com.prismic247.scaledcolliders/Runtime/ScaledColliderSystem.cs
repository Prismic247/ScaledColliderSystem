using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Prismic247.ScaledColliders {
    [DefaultExecutionOrder(-1), UdonBehaviourSyncMode(BehaviourSyncMode.None), DisallowMultipleComponent, RequireComponent(typeof(CapsuleCollider)), RequireComponent(typeof(Rigidbody))]
    public class ScaledColliderSystem : UdonSharpBehaviour {
        [Header("Scaled Collider System")]
        [Tooltip("Enables/disables the scaled collider system.\n\nCall 'ToggleColliders(bool)' instead if changing this during runtime.\n\nPrefab can be found at https://github.com/Prismic247/ScaledColliderSystem/")]
        public bool enableScaledColliders = true;
        [Tooltip("The game object parent which contains the parts of the world intended to be colliably scaled.\n\nFor performance reasons only include collidable objects that need to be scaled.\n\nDo not use this prefab as the parent object.")]
        public GameObject worldParent;
        [Tooltip("The player eye height in meters where the relative world scale should be considerd 1-to-1. Everything scales relative to this value.\n\nDefault: 1.6")]
        [Range(0.3f, 20f)]
        public float baseEyeHeight = 1.6f;
        [Header("Player Size Settings")]
        [Tooltip("Allow players to manually set their own scale, bounded by 'Minimum Scale' and 'Maximum Scale'. Reapplies on join and on respawn.")]
        public bool manualScalingAllowed = true;
        [Tooltip("The smallest eye height in meters that players can manually set their scale to, controlled by 'Manual Scaling Allowed'. Reapplies on join and on respawn.\n\nWARNING: Lower scales cause some degree of collision instability, which gets worse the smaller you go. Recommended minimum is 0.3m.\n\nDefault: 0.3")]
        [Range(0.3f, 20f)]
        public float minimumEyeHeight = 0.3f;
        [Tooltip("The largest eye height in meters that players can manually set their scale to, controlled by 'Manual Scaling Allowed'. Reapplies on join and on respawn.\n\nDefault: 5")]
        [Range(0.3f, 20f)]
        public float maximumEyeHeight = 5f;
        [Header("Player Movement Settings")]
        [Tooltip("Enables/disables whether or not movement (walking, jumping, fall speed, etc) should be scaled relative to the player scale.")]
        public bool enableScaledMovement = true;
        [Tooltip("The player walking speed, which will be scaled based on the player's current eye height relative to 'Base Player Height'.\n\nDefault: 2")]
        public float baseWalkSpeed = 2;
        [Tooltip("The player run speed, which will be scaled based on the player's current eye height relative to 'Base Player Height'.\n\nDefault: 4")]
        public float baseRunSpeed = 4;
        [Tooltip("The player strafing speed, which will be scaled based on the player's current eye height relative to 'Base Player Height'.\n\nDefault: 2")]
        public float baseStrafeSpeed = 2;
        [Tooltip("The player jump impulse, which will be scaled based on the player's current eye height relative to 'Base Player Height'.\n\nDefault: 3")]
        public float baseJumpImpulse = 3;
        [Tooltip("The player gravity, which will be scaled based on the player's current eye height relative to 'Base Player Height'.\n\nDefault: 1")]
        public float basePlayerGravity = 1;
        [Header("Player Sound Settings")]
        [Tooltip("Enables/disables whether or not sounds from the player (voice and avatar audio) should be scaled relative to the player scale.")]
        public bool enableScaledSounds = true;
        [Tooltip("How many meters away that voices can be heard, which will be scaled based on the player's current eye height relative to 'Base Player Height'.\n\nDefault: 25")]
        public float baseVoiceDistance = 25;
        [Tooltip("How many meters away that avatar audio can be heard, which will be scaled based on the player's current eye height relative to 'Base Player Height'.\n\nDefault: 40")]
        public float baseAvatarAudioDistance = 40;
        [Header("Debug Options")]
        [Tooltip("Displays a material specified on 'Ghost Material' on each collider object that usually has a mesh, for debugging.\n\nCall 'ToggleGhosts(bool)' instead if changing this during runtime.")]
        public bool showColliderGhosts = false;
        [Tooltip("A ghost material to show on the invisible colliders, used with 'Show Collider Ghosts' for debugging.\n\nIf used, recommended is a semi-transparent material.")]
        public Material ghostMaterial;
        private GameObject virtualWorld;
        private VRCPlayerApi local;
        private Vector3 playerPosLast = Vector3.zero, playerVelLast = Vector3.zero;
        private Quaternion playerRotLast = Quaternion.identity;
        private Rigidbody rigidBody;
        private int collisionMask = 0b10000000000;
        private float playerScale = 1, worldScale = 1;

        void Start() {
            local = Networking.LocalPlayer;
            if (local == null) { return; }
            local.SetManualAvatarScalingAllowed(manualScalingAllowed);
            local.SetAvatarEyeHeightMinimumByMeters(minimumEyeHeight);
            local.SetAvatarEyeHeightMaximumByMeters(maximumEyeHeight);
            SetLocalMovement();
            rigidBody = gameObject.GetComponent<Rigidbody>();
            rigidBody.useGravity = false;
            rigidBody.rotation = Quaternion.identity;
            rigidBody.freezeRotation = true;
            CapsuleCollider capsuleCollider = gameObject.GetComponent<CapsuleCollider>();
            capsuleCollider.height = 1.6f;
            capsuleCollider.radius = 0.2f;
            capsuleCollider.center = new Vector3(0, 0.8f, 0);
            InitializeScaledColliders();
        }
        
        public override void OnPlayerJoined(VRCPlayerApi player) {
            if (player == null) {
                return;
            } else if (player.isLocal) {
                player.SetManualAvatarScalingAllowed(manualScalingAllowed);
                player.SetAvatarEyeHeightMinimumByMeters(minimumEyeHeight);
                player.SetAvatarEyeHeightMaximumByMeters(maximumEyeHeight);
                AdjustLocalScale();
            } else {
                AdjustRemoteScale(player);
            }
        }

        public override void OnPlayerRespawn(VRCPlayerApi player) {
            if (player == null) {
                return;
            } else if (player.isLocal) {
                player.SetManualAvatarScalingAllowed(manualScalingAllowed);
                player.SetAvatarEyeHeightMinimumByMeters(minimumEyeHeight);
                player.SetAvatarEyeHeightMaximumByMeters(maximumEyeHeight);
                AdjustLocalScale();
            } else {
                AdjustRemoteScale(player);
            }
        }

        public void FixedUpdate() {
            if (local == null || !local.IsValid() || virtualWorld == null || !enableScaledColliders) { return; }
            #if UNITY_EDITOR
            AdjustLocalScale();
            #endif
            playerPosLast = local.GetPosition();
            playerRotLast = local.GetRotation();
            playerVelLast = local.GetVelocity();
            MoveAround(worldParent, virtualWorld, playerPosLast);
            rigidBody.MovePosition(playerPosLast);
        }

        public void OnCollisionEnter(Collision collision) {
            if (playerScale < 1) {InterceptMovement(collision);}
        }

        private void InterceptMovement(Collision collision) {
            local.TeleportTo((playerPosLast + local.GetPosition()) / 2, local.GetRotation());
            local.SetVelocity(RemoveComponent(local.GetVelocity(), collision.impulse));
        }

        public void OnCollisionExit(Collision collision) {
            if (playerScale < 1) {DampenMovement();}
        }

        private void DampenMovement() {
            Vector3 currVel = playerVelLast;
            float horzMag = Mathf.Sqrt(currVel.x * currVel.x + currVel.z * currVel.z);
            float scaledSpeed = local.GetWalkSpeed();
            if (horzMag > scaledSpeed) {
                local.TeleportTo((playerPosLast + local.GetPosition()) / 2, playerRotLast);
                Vector3 relVel = new Vector3(currVel.x / horzMag * scaledSpeed, currVel.y, currVel.z / horzMag * scaledSpeed);
                local.SetVelocity(relVel);
            }
        }

        private Vector3 RemoveComponent(Vector3 vector, Vector3 direction) {
            direction = direction.normalized;
            return vector - direction * Vector3.Dot(vector, direction);
        }

        public override void OnAvatarChanged(VRCPlayerApi player) {
            if (player == null) {
                return;
            } else if (player.isLocal) {
                AdjustLocalScale();
            } else {
                AdjustRemoteScale(player);
            }
        }

        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters) {
            if (player == null) {
                return;
            } else if (player.isLocal) {
                AdjustLocalScale();
            } else {
                AdjustRemoteScale(player);
            }
        }

        public float GetPlayerScale() {return playerScale;}
        public float GetWorldScale() {return worldScale;}

        private void AdjustLocalScale() {
            playerScale = local.GetAvatarEyeHeightAsMeters() / baseEyeHeight;
            worldScale = 1 / playerScale;
            SetLocalMovement();
            if (!enableScaledColliders || worldParent == null) { return; }
            virtualWorld.transform.localScale = new Vector3(worldScale, worldScale, worldScale);
            MoveAround(worldParent, virtualWorld, local.GetPosition());

        }

        private void AdjustRemoteScale(VRCPlayerApi player) {
            float remoteScale = player.GetAvatarEyeHeightAsMeters() / baseEyeHeight;
            if (enableScaledSounds) {
                player.SetVoiceDistanceFar(remoteScale * baseVoiceDistance);
                player.SetAvatarAudioFarRadius(remoteScale * baseAvatarAudioDistance);
            }
        }

        private void SetLocalMovement() {
            float scale = enableScaledMovement ? playerScale : 1;
            local.SetWalkSpeed(baseWalkSpeed * scale);
            local.SetRunSpeed(baseRunSpeed * scale);
            local.SetStrafeSpeed(baseStrafeSpeed * scale);
            local.SetJumpImpulse(baseJumpImpulse * scale);
            local.SetGravityStrength(basePlayerGravity * scale);
        }

        private void MoveAround(GameObject real, GameObject target, Vector3 pivot) {
            Vector3 newPos = pivot + (real.transform.localPosition - pivot) * worldScale / real.transform.localScale.x;
            target.transform.localPosition = newPos;
        }

        public bool ToggleColliderGhosts(bool state) {
            showColliderGhosts = state;
            if (enableScaledColliders) {
                InitializeScaledColliders();
            }
            return showColliderGhosts;
        }

        public bool ToggleScaledColliders(bool state) {
            if (worldParent == null) {
                return enableScaledColliders = false;
            }
            enableScaledColliders = state;
            if (!enableScaledColliders) {
                virtualWorld.transform.localScale = Vector3.one;
                virtualWorld.transform.localPosition = worldParent.transform.position;
                PrepareWorldParent(worldParent, true);
                if (virtualWorld != null) {Destroy(virtualWorld);}
            } else {
                InitializeScaledColliders();
            }
            return state;
        }

        public void InitializeScaledColliders(GameObject newWorldParent = null) {
            if (worldParent == null && (newWorldParent == null || newWorldParent == gameObject)) {
                enableScaledColliders = false;
                return;
            } else if (worldParent != null && newWorldParent != null && newWorldParent != gameObject) {
                PrepareWorldParent(worldParent, true);
                worldParent = newWorldParent;
            } else if (worldParent == null) {worldParent = newWorldParent;}
            if (virtualWorld != null) {Destroy(virtualWorld);}
            virtualWorld = Instantiate(worldParent);
            PrepareWorldParent(worldParent);
            PrepareVirtualWorld(virtualWorld);
            AdjustLocalScale();
        }

        private void PrepareWorldParent(GameObject obj, bool reset = false) {
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++) {
                if (colliders[i].isTrigger || colliders[i].GetType().ToString() == "UnityEngine.TerrainCollider") { continue; }
                if (colliders[i].name.EndsWith("|-|")) {
                    colliders[i].excludeLayers = 0;
                    colliders[i].name = colliders[i].name.Substring(0, colliders[i].name.Length - 3);
                } else if (colliders[i].name.EndsWith("|~|")) {
                    colliders[i].excludeLayers = colliders[i].excludeLayers & ~collisionMask;
                    colliders[i].name = colliders[i].name.Substring(0, colliders[i].name.Length - 3);
                } else if (colliders[i].name.EndsWith("|=|")) {
                    colliders[i].name = colliders[i].name.Substring(0, colliders[i].name.Length - 3);
                }
                if (!reset) {
                    bool hadExcludedLayers = colliders[i].excludeLayers > 0;
                    bool hadExcludedThisLayer = (colliders[i].excludeLayers & collisionMask) == collisionMask;
                    colliders[i].excludeLayers = colliders[i].excludeLayers | collisionMask;
                    colliders[i].name += hadExcludedLayers ? hadExcludedThisLayer ? "|=|" : "|~|" : "|-|";
                }
            }
        }

        private void PrepareVirtualWorld(GameObject obj) {
            Component[] components = obj.GetComponentsInChildren<Component>();
            for (int i = 0; i < components.Length; i++) {
                if (components[i] == null || components[i].gameObject == null) {
                    continue;
                }
                string componentType = components[i].GetType().ToString();
                if (componentType == "UnityEngine.Transform") {
                    continue;
                } else if (componentType == "UnityEngine.RectTransform") {
                    Destroy(components[i].gameObject);
                } else if (componentType.StartsWith("UnityEngine.") && componentType.EndsWith("Collider")) {
                    if (componentType == "UnityEngine.TerrainCollider"){
                        Destroy(components[i]);
                    } else {
                        ((Collider)components[i]).excludeLayers = ~collisionMask;
                        ((Collider)components[i]).includeLayers = collisionMask;
                    }
                } else if (showColliderGhosts && ghostMaterial != null) {
                    if (componentType == "UnityEngine.MeshRenderer") {
                        ((MeshRenderer)components[i]).shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        ((MeshRenderer)components[i]).receiveShadows = false;
                        int materialCount = ((MeshRenderer)components[i]).materials.Length;
                        Material[] newMaterials = new Material[materialCount];
                        for (int j = 0; j < materialCount; j++) {
                            newMaterials[j] = ghostMaterial;
                        }
                        ((MeshRenderer)components[i]).materials = newMaterials;
                    } else if (componentType == "UnityEngine.MeshFilter") {
                        continue;
                    } else {
                        Destroy(components[i]);
                    }
                } else {
                    Destroy(components[i]);
                }
            }
        }
    }
}
