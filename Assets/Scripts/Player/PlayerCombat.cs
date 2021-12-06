using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[RequireComponent (typeof(Rigidbody))]
[RequireComponent (typeof(UnitState))]
public class PlayerCombat : MonoBehaviour, IDamagable<DamageObject> {

	[Header ("Linked Components")]
	public Transform weaponBone; //the bone were weapon will be parented on
	private UnitAnimator animator; //link to the animator component
	private UnitState playerState; //主角状态
	private Rigidbody rb;

	[Header("Attack Data & Combos")]
	public float hitZRange = 2f; //攻击范围
	private int attackNum = -1; //当前攻击组合号
	[Space(5)]

	public DamageObject[] PunchCombo; //拳头攻击列表
	public DamageObject[] KickCombo; //踢腿攻击列表
	public DamageObject JumpKickData; //跳踢攻击
	public DamageObject GroundPunchData; //地面拳头攻击
	public DamageObject GroundKickData; //地面踢腿攻击
	public DamageObject RunningPunch; //跑步拳头攻击
	public DamageObject RunningKick; //跑步踢腿攻击
	private DamageObject lastAttack; //上次发生的攻击的数据

	[Header("Settings")]
	public bool blockAttacksFromBehind = false; //阻挡敌人从背后发起的攻击
	public bool comboContinueOnHit = true; //仅在上一次攻击命中时继续连击
	public bool resetComboChainOnChangeCombo; //切换到其他组合链时重新启动组合
	public bool invulnerableDuringJump = false; //检查玩家在跳跃时是否会被击中
	public float hitRecoveryTime = .4f; //从打击中恢复所需的时间
	public float hitThreshold = .2f; //在我们再次被击中之前的时间
	public float hitKnockBackForce = 1.5f; //我们被击中时的击退力
	public float GroundAttackDistance = 1.5f; //与敌人的距离，在此距离上可以进行地面攻击
	public int knockdownHitCount = 3; //玩家在被击倒之前可以被击中的次数
	public float KnockdownTimeout = 0; //我们被击倒后站起来之前的时间
	public float KnockdownUpForce = 5; //击倒的向上力
	public float KnockbackForce = 4; //击倒的水平力
	public float KnockdownStandUpTime = .8f; //站立动画完成所需的时间

	[Header("Audio")]
	public string knockdownVoiceSFX = "";
	public string hitVoiceSFX = "";
	public string deathVoiceSFX = "";
	public string defenceHitSFX = "";
	public string dropSFX = "";

	[Header ("Stats")]
	public DIRECTION currentDirection; //当前方向
	public GameObject itemInRange; //当前在可交互范围内的item
	private Weapon currentWeapon; //玩家当前持有的武器
	private DIRECTION defendDirection; //防守时的方向
	private bool continuePunchCombo; //如果冲击组合需要继续，则为true
	private bool continueKickCombo; //如果踢组合需要继续，则为true
	private float lastAttackTime = 0; //上次攻击的时间
	[SerializeField]
	private bool targetHit; //如果上次命中目标，则为true
	private int hitKnockDownCount = 0; //玩家连续被击中的次数
	private int hitKnockDownResetTime = 2; //击倒计数器重置前的时间
	private float LastHitTime = 0; //上次我们被击中的时间
	private bool isDead = false; //如果该玩家已死亡，则为true
	private int EnemyLayer; // 敌方
	private int DestroyableObjectLayer; // 可破坏对象层
	private int EnvironmentLayer; //环境层
	private LayerMask HitLayerMask; // 所有可命中对象的列表
	private bool isGrounded;
	private Vector3 fixedVelocity;
	private bool updateVelocity;
	private string lastAttackInput;
	private DIRECTION lastAttackDirection;

	//玩家可以攻击的状态列表
	private List<UNITSTATE> AttackStates = new List<UNITSTATE> {
		UNITSTATE.IDLE, 
		UNITSTATE.WALK, 
		UNITSTATE.RUN, 
		UNITSTATE.JUMPING, 
		UNITSTATE.PUNCH,
		UNITSTATE.KICK, 
		UNITSTATE.DEFEND,
	};

	//玩家可能被击中的状态列表
	private List<UNITSTATE> HitableStates = new List<UNITSTATE> {
		UNITSTATE.DEFEND,
		UNITSTATE.HIT,
		UNITSTATE.IDLE,
		UNITSTATE.LAND,
		UNITSTATE.PUNCH,
		UNITSTATE.KICK,
		UNITSTATE.THROW,
		UNITSTATE.WALK,
		UNITSTATE.RUN,
		UNITSTATE.GROUNDKICK,
		UNITSTATE.GROUNDPUNCH,
	};

	//玩家可以激活防御的状态列表
	private List<UNITSTATE> DefendStates = new List<UNITSTATE> {
		UNITSTATE.IDLE,
		UNITSTATE.DEFEND,
		UNITSTATE.WALK,
		UNITSTATE.RUN,
	};

	//玩家可以改变方向的状态列表
	private List<UNITSTATE> MovementStates = new List<UNITSTATE> {
		UNITSTATE.IDLE,
		UNITSTATE.WALK,
		UNITSTATE.RUN,
		UNITSTATE.JUMPING,
		UNITSTATE.JUMPKICK,
		UNITSTATE.LAND,
		UNITSTATE.DEFEND,
	};

	//---

	void OnEnable(){
		InputManager.onInputEvent += OnInputEvent;
		InputManager.onDirectionInputEvent += OnDirectionInputEvent;
	}

	void OnDisable() {
		InputManager.onInputEvent -= OnInputEvent;
		InputManager.onDirectionInputEvent -= OnDirectionInputEvent;
	}

	//awake
	void Start() {
		animator = GetComponentInChildren<UnitAnimator>();
		playerState = GetComponent<UnitState>();
		rb = GetComponent<Rigidbody>();

		//assign layers and layermasks
		EnemyLayer = LayerMask.NameToLayer("Enemy");
		DestroyableObjectLayer = LayerMask.NameToLayer("DestroyableObject");
		EnvironmentLayer = LayerMask.NameToLayer("Environment");
		HitLayerMask = (1 << EnemyLayer) | (1 << DestroyableObjectLayer);

		//display error messages for missing components
		if (!animator) Debug.LogError ("No player animator found inside " + gameObject.name);
		if (!playerState) Debug.LogError ("No playerState component found on " + gameObject.name);
		if (!rb) Debug.LogError ("No rigidbody component found on " + gameObject.name);

		//set invulnerable during jump
		if (!invulnerableDuringJump) {
			HitableStates.Add (UNITSTATE.JUMPING);
			HitableStates.Add (UNITSTATE.JUMPKICK);
		}
	}

	void Update() {
		
		//the player is colliding with the ground
		if(animator) isGrounded = animator.animator.GetBool("isGrounded");

		//update defence state every frame
		Defend(InputManager.defendKeyDown);
	}

	//physics update
	void FixedUpdate(){
		if (updateVelocity){
			rb.velocity = fixedVelocity;
			updateVelocity = false;
		}
	}

	//late Update
	void LateUpdate(){

		//apply any root motion offsets to parent
		if(animator && animator.GetComponent<Animator>().applyRootMotion && animator.transform.localPosition != Vector3.zero) {
			Vector3 offset = animator.transform.localPosition;
			animator.transform.localPosition = Vector3.zero;
			transform.position += offset * -(int)currentDirection;
		}
	}

	//set velocity in next fixed update
	void SetVelocity(Vector3 velocity){
		fixedVelocity = velocity;
		updateVelocity = true;
	}
		
	//movement input event
	void OnDirectionInputEvent(Vector2 inputVector, bool doubleTapActive){
		if(!MovementStates.Contains(playerState.currentState)) return;
		int dir = Mathf.RoundToInt(Mathf.Sign((float)-inputVector.x));
		if(Mathf.Abs(inputVector.x)>0) currentDirection = (DIRECTION)dir;
	}

	#region Combat Input Events
	//战斗Input事件
	private void OnInputEvent(string action, BUTTONSTATE buttonState) {
		if (AttackStates.Contains (playerState.currentState) && !isDead) {

			//奔跑拳击
			if(action == "Punch" && buttonState == BUTTONSTATE.PRESS && playerState.currentState == UNITSTATE.RUN && isGrounded){
				animator.SetAnimatorBool("Run", false);
				if(RunningPunch.animTrigger.Length>0) doAttack(RunningPunch, UNITSTATE.ATTACK, "Punch");
				return;
			}

			//奔跑踢腿
			if(action == "Kick" && buttonState == BUTTONSTATE.PRESS && playerState.currentState == UNITSTATE.RUN && isGrounded){
				animator.SetAnimatorBool("Run", false);
				if(RunningKick.animTrigger.Length>0) doAttack(RunningKick, UNITSTATE.ATTACK, "Kick");
				return;
			}

			//与范围内的物品交互
			if (action == "Punch" && buttonState == BUTTONSTATE.PRESS && itemInRange != null && isGrounded && currentWeapon == null) {
				interactWithItem();
				return;
			}

			//使用武器
			if (action == "Punch" && buttonState == BUTTONSTATE.PRESS && isGrounded && currentWeapon != null) {
				useCurrentWeapon();
				return;
			}

			//地面拳击
			if (action == "Punch" && buttonState == BUTTONSTATE.PRESS && (playerState.currentState != UNITSTATE.PUNCH && NearbyEnemyDown()) && isGrounded) {
				if(GroundPunchData.animTrigger.Length > 0) doAttack(GroundPunchData, UNITSTATE.GROUNDPUNCH, "Punch");
				return;
			}

			//地面踢腿
			if (action == "Kick" && buttonState == BUTTONSTATE.PRESS && (playerState.currentState != UNITSTATE.KICK && NearbyEnemyDown()) && isGrounded) {
				if(GroundKickData.animTrigger.Length > 0) doAttack(GroundKickData, UNITSTATE.GROUNDKICK, "Kick");
				return;
			}

			//切换到其他组合链时重置组合（用户设置）
			if (resetComboChainOnChangeCombo && (action != lastAttackInput)){
				attackNum = -1;
			}

            //默认拳击
            if (action == "Punch" && buttonState == BUTTONSTATE.PRESS && playerState.currentState != UNITSTATE.PUNCH && playerState.currentState != UNITSTATE.KICK && isGrounded)
            {

                //如果时间在组合窗口内，则继续下一次攻击
                bool insideComboWindow = (lastAttack != null && (Time.time < (lastAttackTime + lastAttack.duration + lastAttack.comboResetTime)));
                if (insideComboWindow && !continuePunchCombo && (attackNum < PunchCombo.Length - 1))
                {
                    attackNum += 1;
                }
                else
                {
                    attackNum = 0;
                }

                if (PunchCombo[attackNum] != null && PunchCombo[attackNum].animTrigger.Length > 0) doAttack(PunchCombo[attackNum], UNITSTATE.PUNCH, "Punch");
                return;
            }

            //如果在拳击攻击中按下“punch”，则推进拳击组合
            if (action == "Punch" && buttonState == BUTTONSTATE.PRESS && (playerState.currentState == UNITSTATE.PUNCH) && !continuePunchCombo && isGrounded)
            {
                if (attackNum < PunchCombo.Length - 1)
                {
                    continuePunchCombo = true;
                    continueKickCombo = false;
                    return;
                }
            }

            //跳拳击
            if (action == "Punch" && buttonState == BUTTONSTATE.PRESS && !isGrounded) {
				if(JumpKickData.animTrigger.Length > 0) {	
					doAttack(JumpKickData, UNITSTATE.JUMPKICK, "Kick");
					StartCoroutine(JumpKickInProgress());
				}
				return;
			}

			//跳踢腿
			if (action == "Kick" && buttonState == BUTTONSTATE.PRESS && !isGrounded) {
				if(JumpKickData.animTrigger.Length > 0) {
					doAttack(JumpKickData, UNITSTATE.JUMPKICK, "Kick");
					StartCoroutine(JumpKickInProgress());
				}
				return;
			}

			//默认踢腿
			if (action == "Kick" && buttonState == BUTTONSTATE.PRESS && playerState.currentState != UNITSTATE.KICK && playerState.currentState != UNITSTATE.PUNCH && isGrounded) {

				//continue to the next attack if the time is inside the combo window
				bool insideComboWindow = (lastAttack != null && (Time.time < (lastAttackTime + lastAttack.duration + lastAttack.comboResetTime)));
				if (insideComboWindow && !continueKickCombo && (attackNum < KickCombo.Length -1)) {
					attackNum += 1;
				} else {
					attackNum = 0;
				}

				doAttack(KickCombo[attackNum], UNITSTATE.KICK, "Kick");
				return;
			}
				
			//advance the kick combo if "kick" was pressed during a kick attack
			if (action == "Kick" && buttonState == BUTTONSTATE.PRESS && (playerState.currentState == UNITSTATE.KICK) && !continueKickCombo && isGrounded) {
				if (attackNum < KickCombo.Length - 1){
					continueKickCombo = true;
					continuePunchCombo = false;
					return;
				}
			}

		}
	}
	#endregion

	#region Combat functions

	private void doAttack(DamageObject damageObject, UNITSTATE state, string inputAction){
		animator.SetAnimatorTrigger(damageObject.animTrigger);
		playerState.SetState(state);

		//save attack data
		lastAttack = damageObject;
		lastAttack.inflictor = gameObject;
		lastAttackTime = Time.time;
		lastAttackInput = inputAction;
		lastAttackDirection = currentDirection;

		//转向当前输入方向
		TurnToDir(currentDirection);

		if(isGrounded) SetVelocity(Vector3.zero);
		if(damageObject.forwardForce>0) animator.AddForce(damageObject.forwardForce);

		if(state == UNITSTATE.JUMPKICK)	return;
		Invoke ("Ready", damageObject.duration);
	}

	//使用现有装备的武器
	void useCurrentWeapon(){
		playerState.SetState (UNITSTATE.USEWEAPON);
		TurnToDir(currentDirection);
		SetVelocity(Vector3.zero);
	
		//save attack data
		lastAttackInput = "WeaponAttack";
		lastAttackTime = Time.time;
		lastAttack = currentWeapon.damageObject;
		lastAttack.inflictor = gameObject;
		lastAttackDirection = currentDirection;

		if(!string.IsNullOrEmpty(currentWeapon.damageObject.animTrigger)) animator.SetAnimatorTrigger(currentWeapon.damageObject.animTrigger);
		if(!string.IsNullOrEmpty(currentWeapon.useSound)) GlobalAudioPlayer.PlaySFX(currentWeapon.useSound);
		Invoke ("Ready", currentWeapon.damageObject.duration);

		//weapon degeneration
		if(currentWeapon.degenerateType == DEGENERATETYPE.DEGENERATEONUSE) currentWeapon.useWeapon();
		if(currentWeapon.degenerateType == DEGENERATETYPE.DEGENERATEONUSE && currentWeapon.timesToUse == 0) StartCoroutine(destroyCurrentWeapon(currentWeapon.damageObject.duration));
		if(currentWeapon.degenerateType == DEGENERATETYPE.DEGENERATEONHIT && currentWeapon.timesToUse == 1) StartCoroutine(destroyCurrentWeapon(currentWeapon.damageObject.duration));
	}

	//移除当前武器
	IEnumerator destroyCurrentWeapon(float delay){
		yield return new WaitForSeconds(delay);
		if(currentWeapon.degenerateType == DEGENERATETYPE.DEGENERATEONUSE) GlobalAudioPlayer.PlaySFX(currentWeapon.breakSound);
		Destroy(currentWeapon.playerHandPrefab);
		currentWeapon.BreakWeapon();
		currentWeapon = null;
	}

	//returns the current weapon
	public Weapon GetCurrentWeapon(){
		return currentWeapon;
	}

	//跳踢进行中
	IEnumerator JumpKickInProgress(){
		animator.SetAnimatorBool ("JumpKickActive", true);

		//a list of enemies that we have hit
		List<GameObject> enemieshit = new List<GameObject>();

		//small delay so the animation has time to play
		yield return new WaitForSeconds(.1f);

		//check for hit
		while (playerState.currentState == UNITSTATE.JUMPKICK) {
			
			//draw a hitbox in front of the character to see which objects it collides with
			Vector3 boxPosition = transform.position + (Vector3.up * lastAttack.collHeight) + Vector3.right * ((int)currentDirection * lastAttack.collDistance);
			Vector3 boxSize = new Vector3 (lastAttack.CollSize/2, lastAttack.CollSize/2, hitZRange/2);
			Collider[] hitColliders = Physics.OverlapBox(boxPosition, boxSize, Quaternion.identity, HitLayerMask); 

			//hit an enemy only once by adding it to a list
			foreach (Collider col in hitColliders) {
				if (!enemieshit.Contains (col.gameObject)) { 
					enemieshit.Add(col.gameObject);

					//hit a damagable object
					IDamagable<DamageObject> damagableObject = col.GetComponent(typeof(IDamagable<DamageObject>)) as IDamagable<DamageObject>;
					if (damagableObject != null) {
						damagableObject.Hit(lastAttack);

						//camera Shake
						CamShake camShake = Camera.main.GetComponent<CamShake> ();
						if (camShake != null) camShake.Shake (.1f);
					}
				}
			}
			yield return null;
		}
	}

	//启动/关闭防御系统
	private void Defend(bool defend){
		if(!DefendStates.Contains(playerState.currentState)) return;
		animator.SetAnimatorBool("Defend", defend);
		if (defend) {
			TurnToDir(currentDirection);
			SetVelocity(Vector3.zero);
			playerState.SetState (UNITSTATE.DEFEND);
			animator.SetAnimatorBool("Run", false); //disable running

		} else{
			if(playerState.currentState == UNITSTATE.DEFEND) playerState.SetState(UNITSTATE.IDLE);
		}
	}

	#endregion

	#region 检查打击

	//检查我们是否击中了什么（动画事件）
	public void CheckForHit() {

		//在角色前面绘制一个命中框，以查看它与哪些对象发生碰撞
		Vector3 boxPosition = transform.position + (Vector3.up * lastAttack.collHeight) + Vector3.right * ((int)lastAttackDirection * lastAttack.collDistance);
		Vector3 boxSize = new Vector3 (lastAttack.CollSize/2, lastAttack.CollSize/2, hitZRange/2);
		Collider[] hitColliders = Physics.OverlapBox(boxPosition, boxSize, Quaternion.identity, HitLayerMask); 

		int i=0;
		while (i < hitColliders.Length) {

			//击中一个可损坏的物体
			IDamagable<DamageObject> damagableObject = hitColliders[i].GetComponent(typeof(IDamagable<DamageObject>)) as IDamagable<DamageObject>;
			if (damagableObject != null) {
				damagableObject.Hit(lastAttack);

				//我们撞到什么东西了
				targetHit = true;
			}
			i++;
		}

		//没有击中任何东西
		if (hitColliders.Length == 0) targetHit = false;

		//武器击中
		if (lastAttackInput == "WeaponAttack" && targetHit) currentWeapon.onHitSomething();
	}

	//在Unity编辑器中显示命中框（调试）
#if UNITY_EDITOR
	void OnDrawGizmos(){
		if (lastAttack != null && (Time.time - lastAttackTime) < lastAttack.duration) {
			Gizmos.color = Color.red;
			Vector3 boxPosition = transform.position + (Vector3.up * lastAttack.collHeight) + Vector3.right * ((int)lastAttackDirection * lastAttack.collDistance);
			Vector3 boxSize = new Vector3 (lastAttack.CollSize, lastAttack.CollSize, hitZRange);
			Gizmos.DrawWireCube (boxPosition, boxSize);
		}
	}
	#endif

	#endregion

	#region We Are Hit

	//we are hit
	public void Hit(DamageObject d) {

		//check if we can get hit again
		if(Time.time < LastHitTime + hitThreshold) return;

		//check if we are in a hittable state
		if (HitableStates.Contains (playerState.currentState)) {
			CancelInvoke();
			 
			//camera Shake
			CamShake camShake = Camera.main.GetComponent<CamShake>();
			if (camShake != null) camShake.Shake(.1f);

			//defend incoming attack
			if(playerState.currentState == UNITSTATE.DEFEND && !d.DefenceOverride && (isFacingTarget(d.inflictor) || blockAttacksFromBehind)) {
				Defend(d);
				return;
			} else {
				animator.SetAnimatorBool("Defend", false);	
			}
				
			//we are hit
			UpdateHitCounter ();
			LastHitTime = Time.time;

			//show hit effect
			animator.ShowHitEffect ();

			//substract health
			HealthSystem healthSystem = GetComponent<HealthSystem>();
			if (healthSystem != null) {
				healthSystem.SubstractHealth (d.damage);
				if (healthSystem.CurrentHp == 0)
					return;
			}

			//check for knockdown
			if ((hitKnockDownCount >= knockdownHitCount || !IsGrounded() || d.knockDown) && playerState.currentState != UNITSTATE.KNOCKDOWN) {
				hitKnockDownCount = 0;
				StopCoroutine ("KnockDownSequence");
				StartCoroutine ("KnockDownSequence", d.inflictor);
				GlobalAudioPlayer.PlaySFXAtPosition (d.hitSFX, transform.position + Vector3.up);
				GlobalAudioPlayer.PlaySFXAtPosition (knockdownVoiceSFX, transform.position + Vector3.up);
				return;
			}

			//default hit
			int i = Random.Range (1, 3);
			animator.SetAnimatorTrigger ("Hit" + i);
			SetVelocity(Vector3.zero);
			playerState.SetState (UNITSTATE.HIT);

			//add a small force from the impact
			if (isFacingTarget(d.inflictor)) { 
				animator.AddForce (-1.5f);
			} else {
				animator.AddForce (1.5f);
			}

			//SFX
			GlobalAudioPlayer.PlaySFXAtPosition (d.hitSFX, transform.position + Vector3.up);
			GlobalAudioPlayer.PlaySFXAtPosition (hitVoiceSFX, transform.position + Vector3.up);

			Invoke("Ready", hitRecoveryTime);
		}
	}

	//update the hit counter
	void UpdateHitCounter() {
		if (Time.time - LastHitTime < hitKnockDownResetTime) { 
			hitKnockDownCount += 1;
		} else {
			hitKnockDownCount = 1;
		}
		LastHitTime = Time.time;
	}

	//defend an incoming attack
	void Defend(DamageObject d){

		//show defend effect
		animator.ShowDefendEffect();

		//play sfx
		GlobalAudioPlayer.PlaySFXAtPosition (defenceHitSFX, transform.position + Vector3.up);

		//add a small force from the impact
		if (isFacingTarget(d.inflictor)) { 
			animator.AddForce (-hitKnockBackForce);
		} else {
			animator.AddForce (hitKnockBackForce);
		}
	}
		
	#endregion

	#region Item interaction

	//item in range
	public void ItemInRange(GameObject item){
		itemInRange = item;
	}

	//item out of range
	public void ItemOutOfRange(GameObject item){
		if(itemInRange == item) itemInRange = null;
	}

	//与范围内的物品交互
	public void interactWithItem(){
		if (itemInRange != null){
			animator.SetAnimatorTrigger ("Pickup");
			playerState.SetState(UNITSTATE.PICKUPITEM);
			SetVelocity(Vector3.zero);
			Invoke ("Ready", .3f);
			Invoke ("pickupItem", .2f);
		}
	}

	//pick up item
	void pickupItem(){
		if(itemInRange != null)	itemInRange.SendMessage("OnPickup", gameObject, SendMessageOptions.DontRequireReceiver);
	}

	//当前装备的武器
	public void equipWeapon(Weapon weapon){
		currentWeapon = weapon;
		currentWeapon.damageObject.inflictor = gameObject;

		//添加玩家手里的武器
		if(weapon.playerHandPrefab != null) {
			GameObject PlayerWeapon = GameObject.Instantiate(weapon.playerHandPrefab, weaponBone) as GameObject;
			currentWeapon.playerHandPrefab = PlayerWeapon;
		}
	}

	#endregion

	#region 击倒序列

	//击倒序列
	public IEnumerator KnockDownSequence(GameObject inflictor) {
		playerState.SetState (UNITSTATE.KNOCKDOWN);
		animator.StopAllCoroutines();
		yield return new WaitForFixedUpdate();

		//look towards the direction of the incoming attack
		int dir = inflictor.transform.position.x > transform.position.x ? 1 : -1;
		currentDirection = (DIRECTION)dir;
		TurnToDir(currentDirection);

		//update playermovement
		var pm = GetComponent<PlayerMovement>();
		if(pm != null) {
			pm.CancelJump();
			pm.SetDirection(currentDirection);
		}

		//add knockback force
		animator.SetAnimatorTrigger("KnockDown_Up");
		while(IsGrounded()){
			SetVelocity(new Vector3 (KnockbackForce * -dir , KnockdownUpForce, 0));
			yield return new WaitForFixedUpdate();
		}

		//going up...
		while(rb.velocity.y >= 0) yield return new WaitForFixedUpdate();

		//going down
		animator.SetAnimatorTrigger ("KnockDown_Down");
		while(!IsGrounded()) yield return new WaitForFixedUpdate();

		//hit ground
		animator.SetAnimatorTrigger ("KnockDown_End");
		CamShake camShake = Camera.main.GetComponent<CamShake>();
		if (camShake != null) camShake.Shake(.3f);
		animator.ShowDustEffectLand();

		//sfx
		GlobalAudioPlayer.PlaySFXAtPosition(dropSFX, transform.position);

		//ground slide
		float t = 0;
		float speed = 2;
		Vector3 fromVelocity = rb.velocity;
		while (t<1){
			SetVelocity(Vector3.Lerp (new Vector3(fromVelocity.x, rb.velocity.y + Physics.gravity.y * Time.fixedDeltaTime, fromVelocity.z), new Vector3(0, rb.velocity.y, 0), t));
			t += Time.deltaTime * speed;
			yield return null;
		}

		//knockDown Timeout
		SetVelocity(Vector3.zero);
		yield return new WaitForSeconds(KnockdownTimeout);

		//stand up
		animator.SetAnimatorTrigger ("StandUp");
		playerState.currentState = UNITSTATE.STANDUP;

		yield return new WaitForSeconds (KnockdownStandUpTime);
		playerState.currentState = UNITSTATE.IDLE;
	}

	#endregion

	//returns true if the closest enemy is in a knockdowngrounded state
	bool NearbyEnemyDown(){
		float distance = GroundAttackDistance;
		GameObject closestEnemy = null;
		foreach (GameObject enemy in EnemyManager.activeEnemies) {

			//only check enemies in front of us
			if(isFacingTarget(enemy)){

				//find closest enemy
				float dist2enemy = (enemy.transform.position - transform.position).magnitude;
				if (dist2enemy < distance) {
					distance = dist2enemy;
					closestEnemy = enemy;
				}
			}
		}
		if (closestEnemy != null) {
			EnemyAI AI = closestEnemy.GetComponent<EnemyAI>();
			if (AI != null && AI.enemyState == UNITSTATE.KNOCKDOWNGROUNDED) {
				return true;
			}
		}
		return false;
	}

	//攻击结束，玩家准备好进行新的动作
	public void Ready() {

		//只有当我们击中某个目标时，才能继续连击
		if (comboContinueOnHit && !targetHit) {
			continuePunchCombo = continueKickCombo = false;
			lastAttackTime = 0;
		}

		//继续拳击组合
		if (continuePunchCombo) {
			continuePunchCombo = continueKickCombo = false;

			if (attackNum < PunchCombo.Length-1) {
				attackNum += 1;
			} else {
				attackNum = 0;
			}
			if(PunchCombo[attackNum] != null && PunchCombo[attackNum].animTrigger.Length>0) doAttack(PunchCombo[attackNum], UNITSTATE.PUNCH, "Punch");
			return;
		}

		//继续踢腿组合
		if (continueKickCombo) {
			continuePunchCombo = continueKickCombo = false;

			if (attackNum < KickCombo.Length-1) {
				attackNum += 1;
			} else {
				attackNum = 0;
			}
			if(KickCombo[attackNum] != null && KickCombo[attackNum].animTrigger.Length>0) doAttack(KickCombo[attackNum], UNITSTATE.KICK, "Kick");
			return;
		}

		playerState.SetState (UNITSTATE.IDLE);
	}

	//如果玩家面对游戏对象，则返回true
	public bool isFacingTarget(GameObject g) {
		return ((g.transform.position.x > transform.position.x && currentDirection == DIRECTION.Left) || (g.transform.position.x < transform.position.x && currentDirection == DIRECTION.Right));
	}

	//如果地面，则返回true
	public bool IsGrounded(){
		CapsuleCollider c = GetComponent<CapsuleCollider> ();
		float colliderSize = c.bounds.extents.y;
		#if UNITY_EDITOR 
		Debug.DrawRay (transform.position + c.center, Vector3.down * colliderSize, Color.red); 
		#endif
		return Physics.Raycast (transform.position + c.center, Vector3.down, colliderSize + .1f, 1 << EnvironmentLayer );
	}

	//turn towards a direction
	public void TurnToDir(DIRECTION dir) {
		transform.rotation = Quaternion.LookRotation(Vector3.forward * -(int)dir);
	}

	//the player has died
	void Death(){
		if (!isDead){
			isDead = true;
			StopAllCoroutines();
			animator.StopAllCoroutines();
			CancelInvoke();
			SetVelocity(Vector3.zero);
			GlobalAudioPlayer.PlaySFXAtPosition(deathVoiceSFX, transform.position + Vector3.up);
			animator.SetAnimatorBool("Death", true);
			EnemyManager.PlayerHasDied();
			StartCoroutine(ReStartLevel());
		}
	}

	//restart this level
	IEnumerator ReStartLevel(){
		yield return new WaitForSeconds(2);
		float fadeoutTime = 1.3f;

		UIManager UI = GameObject.FindObjectOfType<UIManager>();
		if (UI != null){

			//fade out
			UI.UI_fader.Fade (UIFader.FADE.FadeOut, fadeoutTime, 0);
			yield return new WaitForSeconds (fadeoutTime);

			//show game over screen
			UI.DisableAllScreens();
			UI.ShowMenu("GameOver");
		}
	}
}