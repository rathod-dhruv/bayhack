# Stability Coach XR - BayHack

<iframe title="vimeo-player" src="https://player.vimeo.com/video/1137434115?h=1d0765c1f4" width="640" height="360" frameborder="0" referrerpolicy="strict-origin-when-cross-origin" allow="autoplay; fullscreen; picture-in-picture; clipboard-write; encrypted-media; web-share" allowfullscreen></iframe>

## Overview

A mobility coach that prevents falls by guiding seniors through their home using MR, neural haptics, and BCI stress sensing. It adapts to their pace so movement becomes safe, calm, and confident.

## Story

Falls are the leading cause of injury for older adults. When we started this hackathon, we kept coming back to one painful truth: older adults don't fall because they're weak, they fall because they're scared. Fear changes how you move. You hesitate. You freeze. You lose confidence in your own body.

We asked a different question: what if mobility support could understand a person's emotions, not just their steps? What if a system could sense rising stress, slow down guidance, or pause entirely, the same way a real caregiver would?

That's what inspired us to build an XR mobility coach that listens to both the body and the mind. Using passthrough AR to see the environment, Afference's neural haptics to guide movement through touch, and OpenBCI's emotional sensing to adapt in real time, we're creating support that feels human, not technical.

For us, this isn't about making a cool demo. It's about **dignity**. It's about **independence**. It's about making sure another family doesn't get that same terrifying phone call.

## What It Does

Our project is an emotionally adaptive XR mobility coach designed to prevent falls before they happen. Using passthrough AR, neural haptics, and real-time BCI signals, it guides older adults through everyday movements while continuously sensing their stress level and adjusting support on the fly.

### Key Features

- **It sees the room with you**: Using Meta's passthrough and scene understanding, the headset recognizes obstacles like chairs, furniture, or rugs and maps safe paths around them with simple, calming visual cues.

- **It senses how you feel**: OpenBCI measures changes in emotional arousal. If stress rises, the system instantly slows down guidance, shows fewer steps ahead, or pauses entirely to let the user breathe.

- **It guides movement through touch, not menus**: Afference's neural haptic ring gives subtle directional pulses, a gentle nudge to step forward, a soft vibration when too close to a chair, or a slow rhythmic pulse during grounding.

- **It adapts in real time to the user's nervous system**:
  - Calm: Shows more tiles, normal coaching
  - Mild stress: Slows down pacing, reduces visual load
  - High stress: Stops movement, dims UI, activates grounding mode

- **It requires zero tech literacy**: There are no controllers, no buttons, and no gestures to learn. The user simply stands, breathes, and takes small guided steps, and the system does the rest.

## How We Built It

We built the entire experience in **Unity**, which served as the backbone for mixed reality, spatial awareness, and real-time physiological feedback.

### Technology Stack

- **Mixed Reality**: Meta XR SDKs for passthrough, scene understanding, and dynamic scene mesh generation
- **Haptic Feedback**: Afference SDK for neural haptic ring integration
- **EEG/BCI**: OpenBCI Cyton for real-time brainwave data (alpha and beta waves)
- **AI**: Ollama with Meta's Llama 3.2 for stress pattern interpretation and adaptive guidance
- **Audio**: Meta's Text-to-Speech SDK for empathetic voice feedback
- **Development**: Custom C# systems for breathing animations, path spawning, trigger detection, and audio sequencing

## Technologies Used

- C#
- Python
- EEG/BCI (OpenBCI Cyton)
- Neural Haptics (Afference Ring)
- Meta Scene Navigation & Passthrough
- Llama 3.2 AI
- ShaderLab
- HLSL
- JavaScript

## Challenges We Ran Into

- **UX Design**: Making haptics feel supportive, not startling, especially for older adults who can be sensitive to unexpected sensations
- **Software Integration**: OpenBCI connection instability on macOS, preventing early access to EEG data
- **EEG Signal Quality**: Channel interference made alpha-beta readings unreliable for generating meaningful feedback
- **Hardware Reliability**: Cyton connection drops and Afference ring firmware/wiring issues
- **Accessibility**: Designing for elderly users required prioritizing clarity, comfort, and simplicity while keeping the experience non-overwhelming

## Accomplishments

- ✅ Built an end-to-end adaptive loop that reads EEG signals in real time, processes them with an on-device LLM, and delivers personalized visual and audio feedback based on the user's stress level
- ✅ Integrated passthrough, scene understanding, and spatial interaction features from Meta Quest 3
- ✅ Designed the entire flow around older adults, focusing on simplicity, safety, and accessibility

## What We Learned

- Designing for older adults taught us to rethink clarity, pacing, and comfort in every interaction
- Working with real-time EEG signals showed us how unpredictable biosensor data is and why fallback strategies matter
- Coordinating Quest ↔ Laptop ↔ Cyton ↔ Afference Ring required robust error-handling to keep everything in sync

## What's Next

This weekend was just the beginning. We're building a coach that grows with every step a user takes:
- Learning their movement patterns
- Sensing their stress
- Predicting fall risk before it happens

The next version will:
- Adapt entire homes through spatial anchors and smart lighting
- Connect directly with families and therapists so no one has to face fear alone
- Provide personalized AR cues

**Our goal is simple and massive**: use AI, XR, haptics, and emotional sensing to give older adults their independence back, not someday, but now.

## Links

- [DevPost Project](https://devpost.com/software/healthcarexr)
- [BayHack Hackathon](https://www.bayhacksanmateo.com/)

---

**Built with ❤️ for older adults and their families**
