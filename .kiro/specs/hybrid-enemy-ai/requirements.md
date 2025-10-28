# Hybrid Enemy AI Requirements Document

## Introduction

This document defines the requirements for implementing a Hybrid Enemy AI system for the dice poker game. The AI will adapt its behavior based on the current game state (leading vs behind) and employ strategic decision-making to maximize points per turn while managing risk.

## Glossary

- **Hybrid AI**: An AI that switches between aggressive and passive behaviors based on game state
- **Low Combinations**: Combinations with low point values or no multipliers (e.g., single pairs, low straights)
- **High Combinations**: Combinations with high point values or multipliers (e.g., full house, six of a kind)
- **Reroll Strategy**: Using minimum dice for current combination to maximize remaining dice for better combinations
- **Points Per Turn Cap**: Maximum target points the AI aims for in a single turn
- **Iteration Threshold**: Maximum number of reroll attempts before AI stops regardless of results
- **Lead State**: AI has higher total score than player
- **Behind State**: AI has lower total score than player

## Requirements

### Requirement 1: Two-State Hybrid Behavior System

**User Story:** As a game designer, I want the AI to have exactly two distinct states (Aggressive and Passive) based on score buffer, so that it provides clear strategic differences and challenging gameplay.

#### Acceptance Criteria

1. THE Hybrid_AI SHALL have exactly two behavior states: AGGRESSIVE and PASSIVE
2. WHEN the AI total score is lower than player score by buffer amount, THE Hybrid_AI SHALL switch to AGGRESSIVE state
3. WHEN the AI total score is higher than player score by buffer amount, THE Hybrid_AI SHALL switch to PASSIVE state  
4. THE Hybrid_AI SHALL use buffer threshold of ±100 points for state switching
5. THE Hybrid_AI SHALL maintain behavior state consistency within a single turn

### Requirement 2: Hierarchical Combination Evaluation Strategy

**User Story:** As an AI system, I want to evaluate combinations hierarchically from highest to lowest value, so that I can make optimal strategic decisions for maximum points per turn.

#### Acceptance Criteria

1. THE Hybrid_AI SHALL search for combinations starting from highest value in hierarchy
2. WHEN high-value combination found above threshold, THE Hybrid_AI SHALL take it immediately
3. WHEN no high-value combination found, THE Hybrid_AI SHALL search for minimum dice usage combinations
4. THE Hybrid_AI SHALL lower combination value threshold as remaining dice count decreases
5. THE Hybrid_AI SHALL calculate potential points gained for each available combination before selection

### Requirement 3: Aggressive State Reroll Strategy

**User Story:** As an aggressive AI state, I want to maximize points per turn by taking calculated risks and using hierarchical combination evaluation, so that I can catch up to or maintain lead against the player.

#### Acceptance Criteria

1. WHEN in AGGRESSIVE state, THE Hybrid_AI SHALL search hierarchy for high-value combinations first
2. WHEN no high-value combination above threshold found, THE Hybrid_AI SHALL select minimum dice usage combinations
3. THE Hybrid_AI SHALL lower combination value threshold by 20% for each dice count reduction
4. THE Hybrid_AI SHALL continue rerolling until reaching points per turn cap OR momentum system forces stop
5. THE Hybrid_AI SHALL aim for maximum points possible per turn regardless of risk

### Requirement 4: Passive State Conservative Strategy

**User Story:** As a passive AI state, I want to play conservatively to maintain my lead while still scoring efficiently, so that I don't risk losing my advantage through unnecessary risks.

#### Acceptance Criteria

1. WHEN in PASSIVE state, THE Hybrid_AI SHALL use much lower combination value threshold than aggressive state
2. THE Hybrid_AI SHALL have high probability to stop when facing 2 dice situations
3. THE Hybrid_AI SHALL have high stop chance on first iteration with 2 or fewer dice
4. THE Hybrid_AI SHALL still aim for highest points possible but with reduced risk tolerance
5. THE Hybrid_AI SHALL prioritize turn completion over maximum point potential when in passive state

### Requirement 5: Dual Probability Points Per Turn Cap System

**User Story:** As an AI system, I want to have a probabilistic points per turn cap system that works independently from momentum, so that I can make nuanced risk decisions when reaching scoring goals.

#### Acceptance Criteria

1. THE Hybrid_AI SHALL set points per turn cap based on current AI state (AGGRESSIVE: 400-600, PASSIVE: 200-300)
2. WHEN reaching points per turn cap, THE Hybrid_AI SHALL calculate cap stop probability separate from momentum
3. THE Hybrid_AI SHALL perform two independent probability rolls: cap chance and momentum chance
4. THE Hybrid_AI SHALL stop turn when either probability roll succeeds (dual probability system)
5. THE Hybrid_AI SHALL calculate combined stop probability as: 1 - (1-cap_chance) × (1-momentum_chance)

### Requirement 6: Iteration Threshold Management

**User Story:** As an AI system, I want to limit my reroll attempts to prevent infinite loops and excessive risk-taking, so that turns complete in reasonable time.

#### Acceptance Criteria

1. THE Hybrid_AI SHALL set maximum iteration threshold based on behavior mode
2. WHEN aggressive, THE Hybrid_AI SHALL allow up to 5 reroll iterations
3. WHEN conservative, THE Hybrid_AI SHALL allow up to 2 reroll iterations  
4. THE Hybrid_AI SHALL force turn end when reaching iteration threshold
5. THE Hybrid_AI SHALL track and display iteration count for debugging

### Requirement 7: Probability-Based Decision Making

**User Story:** As an AI system, I want to calculate risk probabilities before making decisions, so that I can make informed strategic choices.

#### Acceptance Criteria

1. THE Hybrid_AI SHALL calculate Zonk probability based on remaining dice count
2. THE Hybrid_AI SHALL factor probability into continue/stop decisions
3. WHEN Zonk probability exceeds 60%, THE Hybrid_AI SHALL end turn if in conservative mode
4. WHEN Zonk probability exceeds 80%, THE Hybrid_AI SHALL end turn regardless of mode
5. THE Hybrid_AI SHALL use probability calculations to adjust risk tolerance

### Requirement 8: Non-Physics Dice Generation

**User Story:** As an AI system, I want to generate dice results without physics simulation, so that AI turns execute quickly and efficiently.

#### Acceptance Criteria

1. THE Hybrid_AI SHALL generate dice values through mathematical random generation
2. THE Hybrid_AI SHALL use same probability distribution as physical dice (1/6 per face)
3. THE Hybrid_AI SHALL complete dice generation instantly without animation delays
4. THE Hybrid_AI SHALL validate generated combinations using same rules as player
5. THE Hybrid_AI SHALL display generated dice values in AI-specific UI area

### Requirement 9: Strategic Combination Prioritization

**User Story:** As an AI system, I want to prioritize combinations based on strategic value rather than just point value, so that I can optimize for long-term success.

#### Acceptance Criteria

1. THE Hybrid_AI SHALL rank combinations by strategic value (points per dice used)
2. THE Hybrid_AI SHALL prefer combinations that leave more dice for rerolling
3. THE Hybrid_AI SHALL consider multiplier potential in combination selection
4. THE Hybrid_AI SHALL avoid combinations that use many dice for low points
5. THE Hybrid_AI SHALL maintain dynamic priority list based on current dice state

### Requirement 10: Dynamic Buffer Cap System

**User Story:** As a game designer, I want the AI behavior buffer to tighten over time, so that games become more aggressive and exciting as they progress.

#### Acceptance Criteria

1. THE Hybrid_AI SHALL start with initial buffer cap of 200 points for conservative behavior
2. THE Hybrid_AI SHALL reduce buffer cap by 20 points every 3 rounds completed by both players
3. THE Hybrid_AI SHALL have minimum buffer cap of 50 points to prevent overly aggressive early switching
4. THE Hybrid_AI SHALL recalculate behavior mode using current buffer cap after each round
5. THE Hybrid_AI SHALL display current buffer cap in debug information for tuning

### Requirement 11: Momentum-Based Loop Protection System

**User Story:** As an AI system, I want to use a momentum-based Fibonacci algorithm for loop protection, so that successful combinations reduce stop probability while maintaining risk awareness.

#### Acceptance Criteria

1. THE Hybrid_AI SHALL use Fibonacci sequence multiplied by base percentage for stop chance calculation
2. THE Hybrid_AI SHALL reduce stop chance by 12% per successful combination in current turn (momentum effect)
3. THE Hybrid_AI SHALL apply minimum momentum multiplier of 25% to prevent infinite loops
4. THE Hybrid_AI SHALL increase dice risk multiplier exponentially for 2 or fewer dice remaining
5. THE Hybrid_AI SHALL apply iteration pressure multiplier that increases 20% per iteration after the second

### Requirement 12: Cap Probability Calculation System

**User Story:** As an AI system, I want to calculate cap stop probability based on how much I've exceeded my points per turn target, so that I have increasing likelihood to stop as I achieve my scoring goals.

#### Acceptance Criteria

1. THE Hybrid_AI SHALL calculate cap stop probability based on points over cap threshold
2. WHEN at exactly points per turn cap, THE Hybrid_AI SHALL have base cap stop probability of 30%
3. THE Hybrid_AI SHALL increase cap stop probability by 15% for every 50 points over cap
4. THE Hybrid_AI SHALL cap maximum cap stop probability at 80% to maintain some continuation chance
5. THE Hybrid_AI SHALL apply different cap probability curves for AGGRESSIVE (slower growth) vs PASSIVE (faster growth) states

### Requirement 13: Turn Management Integration

**User Story:** As a game system, I want the AI to integrate seamlessly with existing turn management, so that gameplay flows naturally between player and AI turns.

#### Acceptance Criteria

1. THE Hybrid_AI SHALL integrate with existing GameTurnManager for turn switching
2. THE Hybrid_AI SHALL use same TurnScoreManager for consistent scoring
3. THE Hybrid_AI SHALL respect same game rules and validation as player
4. THE Hybrid_AI SHALL provide clear visual feedback during AI turn execution
5. THE Hybrid_AI SHALL complete turns within reasonable time limits (5-10 seconds maximum)