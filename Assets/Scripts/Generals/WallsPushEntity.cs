using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WallsPushEntity : MonoBehaviour
{
	Collider2D col;
	public LayerMask playerMask;
	public void WallPush()
	{
		//col = Physics2D.OverlapBox((Vector2)transform.position, transform.localScale, playerMask);
		var userData = col?.transform.GetComponent<KinematicMotor2D>();
		col.transform.position += transform.TransformDirection(Vector3.forward) * userData.defaultPolicy.radius;
		//var oe = Physics2D.OverlapCircleAll(transform.position, current.radius, current.enemyMask);
	}
	private void Awake()
	{
		Debug.Log($"This wall {transform.name} is looking {transform.TransformDirection(Vector3.forward)}");
	}
	private void Update()
	{
		col = Physics2D.OverlapBox((Vector2)transform.position, transform.localScale, playerMask);
		if (col)
		{
			Debug.Log($"{transform.name}: Hello again");
			WallPush();
		}
	}
}
