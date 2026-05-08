"""
Quaternion mathematics with comprehensive operations.

All quaternions use Hamilton convention (w, x, y, z) where w is scalar.
Functions are designed to be pure (no side effects) and handle edge cases.
"""

from __future__ import annotations

import math
from typing import List, Optional, Tuple, Sequence
from functools import lru_cache

from ..core.types import Quaternion, EulerAngles, EulerOrder


# =============================================================================
# Constants
# =============================================================================

EPSILON = 1e-12  # For numerical stability checks
SLERP_THRESHOLD = 1e-6  # Below this, use linear interpolation


# =============================================================================
# Basic Operations
# =============================================================================

def normalize(q: Quaternion) -> Optional[Quaternion]:
    """
    Normalize quaternion to unit length.
    
    Returns None if quaternion has zero or near-zero magnitude
    (which would be undefined for rotations).
    
    Args:
        q: Input quaternion
        
    Returns:
        Unit quaternion or None if degenerate
    """
    mag_sq = q.w*q.w + q.x*q.x + q.y*q.y + q.z*q.z
    
    if mag_sq < EPSILON:
        return None
    
    mag = math.sqrt(mag_sq)
    return Quaternion(q.w/mag, q.x/mag, q.y/mag, q.z/mag)


def conjugate(q: Quaternion) -> Quaternion:
    """
    Quaternion conjugate (inverse for unit quaternions).
    
    For unit quaternions, conjugate equals inverse and represents
    the opposite rotation.
    """
    return Quaternion(q.w, -q.x, -q.y, -q.z)


def inverse(q: Quaternion) -> Optional[Quaternion]:
    """
    Quaternion inverse: q^(-1) such that q * q^(-1) = identity.
    
    For unit quaternions, inverse equals conjugate.
    Returns None if quaternion has zero magnitude.
    """
    mag_sq = q.w*q.w + q.x*q.x + q.y*q.y + q.z*q.z
    
    if mag_sq < EPSILON:
        return None
    
    return Quaternion(q.w/mag_sq, -q.x/mag_sq, -q.y/mag_sq, -q.z/mag_sq)


def dot(q1: Quaternion, q2: Quaternion) -> float:
    """
    Quaternion dot product.
    
    For unit quaternions, |dot| = cos(θ/2) where θ is the angle
    between the orientations.
    """
    return q1.w*q2.w + q1.x*q2.x + q1.y*q2.y + q1.z*q2.z


def multiply(q1: Quaternion, q2: Quaternion) -> Quaternion:
    """
    Hamilton quaternion product: q1 * q2.
    
    Represents composition of rotations: first q2, then q1
    (when applied to vectors as q * v * q^(-1)).
    
    Args:
        q1: Left quaternion
        q2: Right quaternion
        
    Returns:
        Product quaternion
        
    Note:
        Result may need normalization after many multiplications
        to counter floating-point drift.
    """
    return Quaternion(
        q1.w*q2.w - q1.x*q2.x - q1.y*q2.y - q1.z*q2.z,
        q1.w*q2.x + q1.x*q2.w + q1.y*q2.z - q1.z*q2.y,
        q1.w*q2.y - q1.x*q2.z + q1.y*q2.w + q1.z*q2.x,
        q1.w*q2.z + q1.x*q2.y - q1.y*q2.x + q1.z*q2.w,
    )


def angular_distance(q1: Quaternion, q2: Quaternion) -> float:
    """
    Angular distance between two orientations in radians.
    
    Returns value in range [0, π].
    """
    d = abs(dot(q1, q2))
    d = min(1.0, d)  # Clamp for numerical stability
    return 2.0 * math.acos(d)


def angular_distance_deg(q1: Quaternion, q2: Quaternion) -> float:
    """Angular distance in degrees."""
    return math.degrees(angular_distance(q1, q2))


# =============================================================================
# Interpolation
# =============================================================================

def slerp(q1: Quaternion, q2: Quaternion, t: float) -> Quaternion:
    """
    Spherical Linear Interpolation between quaternions.
    
    Produces smooth interpolation along the shortest arc on the
    4D unit hypersphere.
    
    Args:
        q1: Start quaternion (t=0)
        q2: End quaternion (t=1)  
        t: Interpolation parameter [0, 1]
        
    Returns:
        Interpolated quaternion (normalized)
        
    Notes:
        - Handles antipodal quaternions (q and -q represent same rotation)
        - Falls back to linear interpolation for nearly-identical quaternions
        - t outside [0,1] extrapolates but may be inaccurate
    """
    # Ensure t is reasonable (allow slight extrapolation)
    t = max(-0.1, min(1.1, t))
    
    # Compute dot product
    d = dot(q1, q2)
    
    # If negative dot, negate one quaternion for shorter path
    if d < 0.0:
        q2 = -q2
        d = -d
    
    # Clamp for numerical stability
    d = min(1.0, max(-1.0, d))
    
    # For nearly identical quaternions, use linear interpolation
    if d > (1.0 - SLERP_THRESHOLD):
        result = Quaternion(
            q1.w + t * (q2.w - q1.w),
            q1.x + t * (q2.x - q1.x),
            q1.y + t * (q2.y - q1.y),
            q1.z + t * (q2.z - q1.z),
        )
        return normalize(result) or Quaternion.identity()
    
    # Standard slerp
    theta = math.acos(d)
    sin_theta = math.sin(theta)
    
    s1 = math.sin((1.0 - t) * theta) / sin_theta
    s2 = math.sin(t * theta) / sin_theta
    
    result = Quaternion(
        s1 * q1.w + s2 * q2.w,
        s1 * q1.x + s2 * q2.x,
        s1 * q1.y + s2 * q2.y,
        s1 * q1.z + s2 * q2.z,
    )
    
    return normalize(result) or Quaternion.identity()


def nlerp(q1: Quaternion, q2: Quaternion, t: float) -> Quaternion:
    """
    Normalized Linear Interpolation.
    
    Faster than slerp but doesn't maintain constant angular velocity.
    Good enough for small angular differences or when speed matters.
    """
    d = dot(q1, q2)
    if d < 0.0:
        q2 = -q2
    
    result = Quaternion(
        q1.w + t * (q2.w - q1.w),
        q1.x + t * (q2.x - q1.x),
        q1.y + t * (q2.y - q1.y),
        q1.z + t * (q2.z - q1.z),
    )
    return normalize(result) or Quaternion.identity()


# =============================================================================
# Averaging (for calibration)
# =============================================================================

def average_simple(quaternions: Sequence[Quaternion]) -> Optional[Quaternion]:
    """
    Simple quaternion average via sign-corrected component averaging.
    
    Fast but only accurate for small angular spread (<30°).
    Use average_iterative() for better accuracy.
    
    Args:
        quaternions: Sequence of quaternions to average
        
    Returns:
        Average quaternion or None if input empty
    """
    if not quaternions:
        return None
    
    if len(quaternions) == 1:
        return quaternions[0]
    
    # Use first as reference for sign alignment
    ref = quaternions[0]
    
    sw = ref.w
    sx = ref.x
    sy = ref.y
    sz = ref.z
    
    for q in quaternions[1:]:
        # Align sign to reference
        if dot(ref, q) < 0.0:
            q = -q
        sw += q.w
        sx += q.x
        sy += q.y
        sz += q.z
    
    return normalize(Quaternion(sw, sx, sy, sz))


def average_iterative(
    quaternions: Sequence[Quaternion],
    max_iterations: int = 20,
    tolerance_rad: float = 1e-6
) -> Optional[Quaternion]:
    """
    Iterative quaternion mean (Karcher/Fréchet mean on S³).
    
    More accurate than simple averaging, especially for
    quaternions with larger angular spread.
    
    Algorithm:
        1. Start with initial estimate (simple average)
        2. Project all quaternions to tangent space at estimate
        3. Compute mean in tangent space
        4. Map back to S³ 
        5. Repeat until convergence
        
    Args:
        quaternions: Sequence of quaternions to average
        max_iterations: Maximum iterations
        tolerance_rad: Convergence threshold in radians
        
    Returns:
        Average quaternion or None if input empty
    """
    if not quaternions:
        return None
    
    n = len(quaternions)
    if n == 1:
        return quaternions[0]
    
    # Initial estimate
    mean = average_simple(quaternions)
    if mean is None:
        return None
    
    for _ in range(max_iterations):
        # Compute tangent vectors (log map)
        tangent_sum = [0.0, 0.0, 0.0]
        
        for q in quaternions:
            # q_rel = mean^(-1) * q
            mean_inv = conjugate(mean)
            q_rel = multiply(mean_inv, q)
            
            # Ensure positive w (shortest path)
            if q_rel.w < 0:
                q_rel = -q_rel
            
            # Log map: quaternion to axis-angle
            # For small angles, this is approximately (x, y, z)
            w_clamped = min(1.0, max(-1.0, q_rel.w))
            theta = 2.0 * math.acos(w_clamped)
            
            if theta < 1e-10:
                # Very small rotation
                tangent_sum[0] += 2.0 * q_rel.x
                tangent_sum[1] += 2.0 * q_rel.y
                tangent_sum[2] += 2.0 * q_rel.z
            else:
                sin_half = math.sqrt(1.0 - w_clamped * w_clamped)
                if sin_half < 1e-10:
                    continue
                factor = theta / sin_half
                tangent_sum[0] += factor * q_rel.x
                tangent_sum[1] += factor * q_rel.y
                tangent_sum[2] += factor * q_rel.z
        
        # Mean tangent vector
        mean_tangent = [t / n for t in tangent_sum]
        
        # Check convergence
        tangent_mag = math.sqrt(sum(t*t for t in mean_tangent))
        if tangent_mag < tolerance_rad:
            break
        
        # Exp map: axis-angle back to quaternion
        if tangent_mag < 1e-10:
            delta = Quaternion.identity()
        else:
            half_angle = tangent_mag / 2.0
            s = math.sin(half_angle) / tangent_mag
            delta = Quaternion(
                math.cos(half_angle),
                s * mean_tangent[0],
                s * mean_tangent[1],
                s * mean_tangent[2],
            )
        
        # Update mean
        mean = multiply(mean, delta)
        mean = normalize(mean) or mean
    
    return mean


def compute_variance(
    quaternions: Sequence[Quaternion],
    mean: Optional[Quaternion] = None
) -> float:
    """
    Compute angular variance of quaternion set.
    
    Returns variance in radians squared.
    Useful for assessing calibration quality.
    """
    if len(quaternions) < 2:
        return 0.0
    
    if mean is None:
        mean = average_iterative(quaternions)
    
    if mean is None:
        return float('inf')
    
    sum_sq = 0.0
    for q in quaternions:
        dist = angular_distance(mean, q)
        sum_sq += dist * dist
    
    return sum_sq / len(quaternions)


def compute_std_deg(
    quaternions: Sequence[Quaternion],
    mean: Optional[Quaternion] = None
) -> float:
    """Compute angular standard deviation in degrees."""
    return math.degrees(math.sqrt(compute_variance(quaternions, mean)))


# =============================================================================
# Euler Angle Conversions
# =============================================================================

def from_euler(euler: EulerAngles) -> Quaternion:
    """
    Convert Euler angles to quaternion.
    
    Supports multiple rotation orders (XYZ, ZYX, etc.) and
    both intrinsic (body-fixed) and extrinsic (space-fixed) conventions.
    """
    rx, ry, rz = euler.angles
    
    # Half angles
    hx, hy, hz = rx / 2.0, ry / 2.0, rz / 2.0
    
    cx, sx = math.cos(hx), math.sin(hx)
    cy, sy = math.cos(hy), math.sin(hy)
    cz, sz = math.cos(hz), math.sin(hz)
    
    # Build rotation quaternions
    qx = Quaternion(cx, sx, 0.0, 0.0)
    qy = Quaternion(cy, 0.0, sy, 0.0)
    qz = Quaternion(cz, 0.0, 0.0, sz)
    
    # Compose based on order
    order_map = {
        EulerOrder.XYZ: (qx, qy, qz),
        EulerOrder.ZYX: (qz, qy, qx),
        EulerOrder.ZXY: (qz, qx, qy),
        EulerOrder.YXZ: (qy, qx, qz),
        EulerOrder.XZY: (qx, qz, qy),
        EulerOrder.YZX: (qy, qz, qx),
    }
    
    q1, q2, q3 = order_map[euler.order]
    
    if euler.intrinsic:
        # Body-fixed: multiply left to right
        result = multiply(multiply(q1, q2), q3)
    else:
        # Space-fixed: multiply right to left  
        result = multiply(multiply(q3, q2), q1)
    
    return result


def from_euler_xyz(rx: float, ry: float, rz: float) -> Quaternion:
    """Convenience function for body-fixed XYZ Euler angles (radians)."""
    return from_euler(EulerAngles((rx, ry, rz), EulerOrder.XYZ, intrinsic=True))


def to_euler(q: Quaternion, order: EulerOrder = EulerOrder.XYZ) -> Tuple[float, float, float]:
    """
    Convert quaternion to Euler angles.
    
    Warning: Euler angles have singularities (gimbal lock).
    Results may be unexpected near singularities.
    
    Returns angles in radians.
    """
    w, x, y, z = q.w, q.x, q.y, q.z
    
    if order == EulerOrder.XYZ:
        # Roll (x-axis rotation)
        sinr_cosp = 2.0 * (w * x + y * z)
        cosr_cosp = 1.0 - 2.0 * (x * x + y * y)
        rx = math.atan2(sinr_cosp, cosr_cosp)
        
        # Pitch (y-axis rotation)
        sinp = 2.0 * (w * y - z * x)
        sinp = max(-1.0, min(1.0, sinp))  # Clamp
        ry = math.asin(sinp)
        
        # Yaw (z-axis rotation)
        siny_cosp = 2.0 * (w * z + x * y)
        cosy_cosp = 1.0 - 2.0 * (y * y + z * z)
        rz = math.atan2(siny_cosp, cosy_cosp)
        
        return (rx, ry, rz)
    
    # Add other orders as needed
    raise NotImplementedError(f"Euler order {order} not yet implemented")


# =============================================================================
# Sign Continuity
# =============================================================================

def ensure_continuity(
    current: Quaternion, 
    previous: Quaternion
) -> Quaternion:
    """
    Ensure quaternion sign continuity with previous sample.
    
    Quaternions q and -q represent the same rotation.
    For smooth interpolation, we want consecutive quaternions
    to have positive dot product (same hemisphere).
    """
    if dot(current, previous) < 0.0:
        return -current
    return current