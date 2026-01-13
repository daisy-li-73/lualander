using Debug = UnityEngine.Debug;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Security.Cryptography;

public class Lander : MonoBehaviour
{
    public static Lander Instance { get; private set; }

    private const float GRAVITY_NORMAL = 0.7f;

    public event EventHandler OnUpForce;
    public event EventHandler OnLeftForce;
    public event EventHandler OnRightForce;
    public event EventHandler OnBeforeForce;
    public event EventHandler OnCoinPickup;
    public event EventHandler<OnStateChangeEventArgs> OnStateChange;
    public class OnStateChangeEventArgs : EventArgs
    {
        public State state;
    }
    public event EventHandler<OnLandedEventArgs> OnLanded;
    public class OnLandedEventArgs : EventArgs
    {
        public LandingType landingType;
        public int score;
        public float landingAngle;
        public float landingSpeed;
        public float scoreMultiplier;

    }

    public enum LandingType
    {
        Success,
        MissedLandingPad,
        LandingAngleTooSteep,
        LandedTooFast,
    }

    public enum State
    {
        WaitingToStart,
        Normal,
        GameOver,
    }

    private float fuel = 10f;
    private float maxFuel = 10f;
    private State state;


    private Rigidbody2D landerRigidbody2D;

    private void Awake()
    {
        Instance = this;

        fuel = maxFuel;
        state = State.WaitingToStart;

        landerRigidbody2D = GetComponent<Rigidbody2D>();
        landerRigidbody2D.gravityScale = 0f;
    }

    // FixedUpdate runs on a set interval (0.02 sec in this case)
        // use FixedUpdate for holds, Update for clicks
    private void FixedUpdate()
    {
        // at beginning of each frame, thrusters are all off
        OnBeforeForce?.Invoke(this, EventArgs.Empty); 

        switch (state)
        {
            default:
            case State.WaitingToStart:
                if (Keyboard.current.upArrowKey.isPressed ||
                   Keyboard.current.leftArrowKey.isPressed ||
                   Keyboard.current.rightArrowKey.isPressed)
                {
                    landerRigidbody2D.gravityScale = GRAVITY_NORMAL;
                    state = State.Normal;
                    SetState(State.Normal);
                }
                    break;
            case State.Normal:
                if (fuel <= 0)
                {
                    return;
                }

                // Pressing any input
                if (Keyboard.current.upArrowKey.isPressed ||
                    Keyboard.current.leftArrowKey.isPressed ||
                    Keyboard.current.rightArrowKey.isPressed)
                {
                    ConsumeFuel();
                }

                if (Keyboard.current.upArrowKey.isPressed)
                {
                    float force = 700f;
                    landerRigidbody2D.AddForce(force * transform.up * Time.deltaTime);
                    // deltaTime keeps force consistent regardless of frame rate (unnecessary in FixedUdpate)
                    OnUpForce?.Invoke(this, EventArgs.Empty);
                }
                if (Keyboard.current.leftArrowKey.isPressed)
                {
                    float turnSpeed = +100f;
                    landerRigidbody2D.AddTorque(turnSpeed * Time.deltaTime);
                    OnLeftForce?.Invoke(this, EventArgs.Empty);
                }
                if (Keyboard.current.rightArrowKey.isPressed)
                {
                    float turnSpeed = -100f;
                    landerRigidbody2D.AddTorque(turnSpeed * Time.deltaTime);
                    OnRightForce?.Invoke(this, EventArgs.Empty);
                }
                break;
            case State.GameOver:
                break;
        }    
    }

    private void ConsumeFuel()
    {
        float fuelConsumption = 1f;
        fuel -= fuelConsumption * Time.deltaTime;
    }

    private void SetState(State state)
    {
        this.state = state;
        OnStateChange?.Invoke(this, new OnStateChangeEventArgs
        {
            state = state,
        });
    }

    private void OnCollisionEnter2D(Collision2D collision2D)
    {
        if (!collision2D.gameObject.TryGetComponent(out LandingPad landingPad)) {
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.MissedLandingPad,
                landingAngle = 0f,
                landingSpeed = 0f,
                scoreMultiplier = 0,
                score = 0,
            });
            SetState(State.GameOver);
            return;
        }

        float maxSoftLandingVelocity = 4f;
        float relativeVelocity = collision2D.relativeVelocity.magnitude;
        // Bad Landing: too fast
        if (relativeVelocity > maxSoftLandingVelocity)
        {
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.LandedTooFast,
                landingAngle = 0f,
                landingSpeed = relativeVelocity,
                scoreMultiplier = 0,
                score = 0,
            });
            SetState(State.GameOver);
            return;
        }

        float dotVector = Vector2.Dot(Vector2.up, transform.up);
        float minDotVector = 0.90f;
        // Bad Landing: crooked landing
        if (minDotVector > dotVector)
        {
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.LandingAngleTooSteep,
                landingAngle = dotVector,
                landingSpeed = relativeVelocity,
                scoreMultiplier = 0,
                score = 0,
            });
            SetState(State.GameOver);
            return;
        }

        Debug.Log("Successful landing!");

        float maxScoreLandingAngle = 100;
        float landingAngleMultiplier = 10f;
        float landingAngleScore = maxScoreLandingAngle - Mathf.Abs(dotVector - 1f) * landingAngleMultiplier * maxScoreLandingAngle;

        float landingSpeedMultiplier = 100;
        float landingSpeedScore = (maxSoftLandingVelocity - relativeVelocity) * landingSpeedMultiplier;

        int score = Mathf.RoundToInt(landingAngleScore + landingSpeedScore) * landingPad.GetScoreMultiplier();

        Debug.Log("Score: " + score);
        OnLanded?.Invoke(this, new OnLandedEventArgs
        {
            landingType = LandingType.Success,
            landingAngle = dotVector,
            landingSpeed = relativeVelocity,
            scoreMultiplier = landingPad.GetScoreMultiplier(),
            score = score,
        });
        SetState(State.GameOver);
    }

    private void OnTriggerEnter2D(Collider2D collider2D )
    {
        if (collider2D.gameObject.TryGetComponent(out FuelPickup fuelPickup) )
        {
            float addFuel = 10f;
            fuel = Mathf.Min(fuel + addFuel, maxFuel);
            fuelPickup.DestroySelf();
        }

        if (collider2D.gameObject.TryGetComponent(out CoinPickup coinPickup))
        {
            OnCoinPickup?.Invoke(this, EventArgs.Empty);
            coinPickup.DestroySelf();
        }
    }

    public float GetSpeedX()
    {
        return landerRigidbody2D.linearVelocityX;
    }

    public float GetSpeedY()
    {
        return landerRigidbody2D.linearVelocityY;
    }

    public float GetFuel()
    {
        return fuel;
    }

    public float GetFuelNormalized()
    {
        return fuel / maxFuel;
    }
}
