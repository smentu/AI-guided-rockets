// GENERATED AUTOMATICALLY FROM 'Assets/PlayerControls.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @PlayerControls : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @PlayerControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerControls"",
    ""maps"": [
        {
            ""name"": ""Gameplay"",
            ""id"": ""0b25628a-c02f-4045-be44-cdf2bf6d397c"",
            ""actions"": [
                {
                    ""name"": ""FireEngine"",
                    ""type"": ""Button"",
                    ""id"": ""9508b2d1-55e1-4b93-9e65-1dece1f94ce6"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""ThrustDirection"",
                    ""type"": ""Value"",
                    ""id"": ""bc43ceb6-7e4a-472b-915e-8e969e76fee7"",
                    ""expectedControlType"": ""Stick"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""AutopilotToggle"",
                    ""type"": ""Button"",
                    ""id"": ""eca1ef2d-ff0b-4546-b5b8-0fcc5fab3a9f"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""CameraMove"",
                    ""type"": ""Value"",
                    ""id"": ""ba23efa6-1dff-4cd6-99bc-326aaedfa440"",
                    ""expectedControlType"": ""Stick"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""LegsToggle"",
                    ""type"": ""Button"",
                    ""id"": ""83daa8c3-74c9-4ba4-b245-2cbf2e1470eb"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""acf797e2-2468-4fa3-825d-3f106f8629af"",
                    ""path"": ""<Gamepad>/buttonSouth"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""FireEngine"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f4df7982-0218-4b49-ae61-e4a80b54d186"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""ThrustDirection"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2f1e0781-ca9f-452e-9e15-345bbbd33279"",
                    ""path"": ""<Gamepad>/start"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""AutopilotToggle"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""eeb98b58-6ba3-456c-a03c-93413a642e3a"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""CameraMove"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""df88def5-8bfc-4764-926d-b82a09c32d9d"",
                    ""path"": ""<Gamepad>/rightShoulder"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""LegsToggle"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Gameplay
        m_Gameplay = asset.FindActionMap("Gameplay", throwIfNotFound: true);
        m_Gameplay_FireEngine = m_Gameplay.FindAction("FireEngine", throwIfNotFound: true);
        m_Gameplay_ThrustDirection = m_Gameplay.FindAction("ThrustDirection", throwIfNotFound: true);
        m_Gameplay_AutopilotToggle = m_Gameplay.FindAction("AutopilotToggle", throwIfNotFound: true);
        m_Gameplay_CameraMove = m_Gameplay.FindAction("CameraMove", throwIfNotFound: true);
        m_Gameplay_LegsToggle = m_Gameplay.FindAction("LegsToggle", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Gameplay
    private readonly InputActionMap m_Gameplay;
    private IGameplayActions m_GameplayActionsCallbackInterface;
    private readonly InputAction m_Gameplay_FireEngine;
    private readonly InputAction m_Gameplay_ThrustDirection;
    private readonly InputAction m_Gameplay_AutopilotToggle;
    private readonly InputAction m_Gameplay_CameraMove;
    private readonly InputAction m_Gameplay_LegsToggle;
    public struct GameplayActions
    {
        private @PlayerControls m_Wrapper;
        public GameplayActions(@PlayerControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @FireEngine => m_Wrapper.m_Gameplay_FireEngine;
        public InputAction @ThrustDirection => m_Wrapper.m_Gameplay_ThrustDirection;
        public InputAction @AutopilotToggle => m_Wrapper.m_Gameplay_AutopilotToggle;
        public InputAction @CameraMove => m_Wrapper.m_Gameplay_CameraMove;
        public InputAction @LegsToggle => m_Wrapper.m_Gameplay_LegsToggle;
        public InputActionMap Get() { return m_Wrapper.m_Gameplay; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(GameplayActions set) { return set.Get(); }
        public void SetCallbacks(IGameplayActions instance)
        {
            if (m_Wrapper.m_GameplayActionsCallbackInterface != null)
            {
                @FireEngine.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnFireEngine;
                @FireEngine.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnFireEngine;
                @FireEngine.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnFireEngine;
                @ThrustDirection.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnThrustDirection;
                @ThrustDirection.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnThrustDirection;
                @ThrustDirection.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnThrustDirection;
                @AutopilotToggle.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnAutopilotToggle;
                @AutopilotToggle.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnAutopilotToggle;
                @AutopilotToggle.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnAutopilotToggle;
                @CameraMove.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraMove;
                @CameraMove.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraMove;
                @CameraMove.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnCameraMove;
                @LegsToggle.started -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLegsToggle;
                @LegsToggle.performed -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLegsToggle;
                @LegsToggle.canceled -= m_Wrapper.m_GameplayActionsCallbackInterface.OnLegsToggle;
            }
            m_Wrapper.m_GameplayActionsCallbackInterface = instance;
            if (instance != null)
            {
                @FireEngine.started += instance.OnFireEngine;
                @FireEngine.performed += instance.OnFireEngine;
                @FireEngine.canceled += instance.OnFireEngine;
                @ThrustDirection.started += instance.OnThrustDirection;
                @ThrustDirection.performed += instance.OnThrustDirection;
                @ThrustDirection.canceled += instance.OnThrustDirection;
                @AutopilotToggle.started += instance.OnAutopilotToggle;
                @AutopilotToggle.performed += instance.OnAutopilotToggle;
                @AutopilotToggle.canceled += instance.OnAutopilotToggle;
                @CameraMove.started += instance.OnCameraMove;
                @CameraMove.performed += instance.OnCameraMove;
                @CameraMove.canceled += instance.OnCameraMove;
                @LegsToggle.started += instance.OnLegsToggle;
                @LegsToggle.performed += instance.OnLegsToggle;
                @LegsToggle.canceled += instance.OnLegsToggle;
            }
        }
    }
    public GameplayActions @Gameplay => new GameplayActions(this);
    public interface IGameplayActions
    {
        void OnFireEngine(InputAction.CallbackContext context);
        void OnThrustDirection(InputAction.CallbackContext context);
        void OnAutopilotToggle(InputAction.CallbackContext context);
        void OnCameraMove(InputAction.CallbackContext context);
        void OnLegsToggle(InputAction.CallbackContext context);
    }
}
