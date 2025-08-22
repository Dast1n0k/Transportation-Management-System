import sqlite3
import math
from datetime import datetime
from app.modules.hris.core.db import get_db
from app.modules.hris.utils.formatters import (
    format_capacity,
    calculate_distance_miles  # Changed to miles
)
from app.modules.hris.constants import (
    CourierErrorCodes,
    SearchSettings,
    VehicleTypes
)


def create_courier_profile(user_id, courier_data):
    """Creates courier profile"""
    with get_db() as conn:
        # Create courier profile
        cur = conn.execute("""
            INSERT INTO courier_profiles (
                user_id, name, surname, phone, vehicle_type, dimensions, capacity,
                zipcode, latitude, longitude, is_available, available_since, notes, location
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, (
            user_id,  # Add user_id as first parameter
            courier_data.get('name', '').strip() or None,
            courier_data.get('surname', '').strip() or None,
            courier_data.get('phone', '').strip() or None,
            courier_data.get('vehicle_type'),
            courier_data.get('dimensions', '').strip() or None,
            courier_data.get('capacity', '').strip() or None,
            courier_data.get('zipcode'),
            courier_data.get('latitude'),
            courier_data.get('longitude'),
            courier_data.get('is_available', False),
            datetime.utcnow() if courier_data.get('is_available') else None,
            courier_data.get('notes', '').strip() or None,
            courier_data.get('location', '').strip() or None
        ))

        courier_id = cur.lastrowid
        conn.commit()

        return get_courier_by_id(courier_id), None


def get_all_couriers(limit=None, offset=None):
    """Gets list of all couriers"""
    with get_db() as conn:
        query = """
            SELECT cp.*, u.username
            FROM courier_profiles cp
            LEFT JOIN users u ON cp.user_id = u.id
            ORDER BY cp.created_at DESC
        """
        params = []

        if limit:
            query += " LIMIT ?"
            params.append(limit)
        if offset:
            query += " OFFSET ?"
            params.append(offset)

        cur = conn.execute(query, params)
        couriers = [dict(row) for row in cur.fetchall()]

        # Get total count
        cur = conn.execute("SELECT COUNT(*) as total FROM courier_profiles")
        total = cur.fetchone()['total']

        return couriers, total


def get_courier_by_id(courier_id):
    """Gets courier by ID"""
    with get_db() as conn:
        cur = conn.execute("""
            SELECT cp.*, u.username
            FROM courier_profiles cp
            LEFT JOIN users u ON cp.user_id = u.id
            WHERE cp.id = ?
        """, (courier_id,))

        courier = cur.fetchone()
        return dict(courier) if courier else None


def get_courier_by_user_id(user_id):
    """Gets courier profile by user_id"""
    with get_db() as conn:
        cur = conn.execute("""
            SELECT cp.*, u.username
            FROM courier_profiles cp
            LEFT JOIN users u ON cp.user_id = u.id
            WHERE cp.user_id = ?
        """, (user_id,))

        courier = cur.fetchone()
        return dict(courier) if courier else None


def safe_strip(value):
    if isinstance(value, str):
        value = value.strip()
        return value or None  # return None if empty string
    return value  # keep None or non-string as-is

def update_courier_profile(courier_id, courier_data):
    """Updates courier profile - Now allows managers to update any courier"""
    # Get existing profile
    existing_courier = get_courier_by_id(courier_id)
    # if not existing_courier:
    #     return None, {
    #         'error': 'Courier not found',
    #         'code': CourierErrorCodes.COURIER_NOT_FOUND
    #     }

    # Check access rights - Now allows managers
    # if not can_modify_courier(existing_courier, current_user):
    #     return None, {
    #         'error': 'Cannot modify other courier profile',
    #         'code': CourierErrorCodes.CANNOT_MODIFY_OTHER_COURIER
    #     }

    with get_db() as conn:
        update_fields = []
        update_values = []

        fields_to_update = [
            ('name', safe_strip(courier_data.get('name'))),
            ('surname', safe_strip(courier_data.get('surname'))),
            ('phone', safe_strip(courier_data.get('phone'))),
            ('vehicle_type', courier_data.get('vehicle_type')),
            ('dimensions', safe_strip(courier_data.get('dimensions'))),
            ('capacity', safe_strip(courier_data.get('capacity'))),
            ('zipcode', courier_data.get('zipcode')),
            ('latitude', courier_data.get('latitude')),
            ('longitude', courier_data.get('longitude')),
            ('notes', safe_strip(courier_data.get('notes'))),
            ('location', safe_strip(courier_data.get('location')))
        ]

        for field, value in fields_to_update:
            if field in courier_data:  # Only update passed fields
                update_fields.append(f"{field} = ?")
                update_values.append(value)

        # Handle availability
        if 'is_available' in courier_data:
            is_available = courier_data.get('is_available', False)
            update_fields.append("is_available = ?")
            update_values.append(is_available)

            if is_available and not existing_courier.get('is_available'):
                # Courier becomes available
                update_fields.append("available_since = ?")
                update_values.append(datetime.utcnow())
            elif not is_available:
                # Courier becomes unavailable
                update_fields.append("available_since = ?")
                update_values.append(None)

        if not update_fields:
            return existing_courier, None

        # Execute update
        update_values.append(courier_id)
        conn.execute(f"""
            UPDATE courier_profiles
            SET {', '.join(update_fields)}
            WHERE id = ?
        """, update_values)

        conn.commit()

        # Return updated profile
        return get_courier_by_id(courier_id), None


def delete_courier_profile(courier_id):
    """Deletes courier profile - Now allows managers to delete"""
    # Get existing profile
    existing_courier = get_courier_by_id(courier_id)

    if not existing_courier:
        return None, {
            'error': 'Courier not found',
            'code': CourierErrorCodes.COURIER_NOT_FOUND
        }

    with get_db() as conn:
        conn.execute(
            "DELETE FROM courier_profiles WHERE id = ?", (courier_id,))
        conn.commit()

    return existing_courier, None


def search_couriers(search_params):
    """Search couriers by various criteria (now using miles)"""

    with get_db() as conn:
        query_parts = ["""
            SELECT cp.*, u.username
            FROM courier_profiles cp
            LEFT JOIN users u ON cp.user_id = u.id
            WHERE 1=1
        """]
        params = []

        # Filter by vehicle type
        if search_params.get('vehicle_type'):
            query_parts.append("AND cp.vehicle_type = ?")
            params.append(search_params['vehicle_type'])

        # Filter by availability
        if search_params.get('is_available') is not None:
            query_parts.append("AND cp.is_available = ?")
            params.append(search_params['is_available'])

        # Filter by zipcode
        if search_params.get('zipcode'):
            query_parts.append("AND cp.zipcode = ?")
            params.append(search_params['zipcode'])

        # Search by name or surname
        if search_params.get('name'):
            query_parts.append("AND (cp.name LIKE ? OR cp.surname LIKE ?)")
            name_pattern = f"%{search_params['name']}%"
            params.extend([name_pattern, name_pattern])

        # Search by phone
        if search_params.get('phone'):
            query_parts.append("AND cp.phone LIKE ?")
            params.append(f"%{search_params['phone']}%")

        # Geo search (now in miles)
        center_lat = search_params.get('center_lat')
        center_lng = search_params.get('center_lng')
        radius = search_params.get(
            'radius', SearchSettings.DEFAULT_SEARCH_RADIUS_MILES)  # Now in miles

        if center_lat is not None and center_lng is not None:
            try:
                center_lat = float(center_lat)
                center_lng = float(center_lng)
                radius = float(radius)

                # Approximate coordinate filter (for optimization)
                lat_range = radius / 69.0
                lng_range = radius / \
                    (69.0 * abs(math.cos(math.radians(center_lat))))

                query_parts.append("""
                    AND cp.latitude BETWEEN ? AND ?
                    AND cp.longitude BETWEEN ? AND ?
                """)
                params.extend([
                    center_lat - lat_range, center_lat + lat_range,
                    center_lng - lng_range, center_lng + lng_range
                ])

            except (ValueError, TypeError):
                pass

        # Sorting
        sort_by = search_params.get('sort_by', 'created_at')
        sort_order = search_params.get('sort_order', 'DESC')

        valid_sort_fields = ['created_at', 'name',
                             'surname', 'vehicle_type', 'is_available']
        if sort_by in valid_sort_fields:
            query_parts.append(f"ORDER BY cp.{sort_by} {sort_order}")
        else:
            query_parts.append("ORDER BY cp.created_at DESC")

        # Limit and offset
        limit = min(int(search_params.get(
            'limit', SearchSettings.DEFAULT_LIMIT)), SearchSettings.MAX_LIMIT)
        offset = int(search_params.get('offset', 0))

        query_parts.append("LIMIT ? OFFSET ?")
        params.extend([limit, offset])

        # Execute query
        cur = conn.execute(' '.join(query_parts), params)
        couriers = [dict(row) for row in cur.fetchall()]

        # If geo search, calculate exact distances and filter (now in miles)
        if center_lat is not None and center_lng is not None and couriers:
            filtered_couriers = []
            for courier in couriers:
                distance = calculate_distance_miles(
                    center_lat, center_lng,
                    courier['latitude'], courier['longitude']
                )
                if distance is not None and distance <= radius:
                    courier['distance_miles'] = round(
                        distance, 2)  # Now in miles
                    filtered_couriers.append(courier)

            # Sort by distance if needed
            if search_params.get('sort_by') == 'distance':
                filtered_couriers.sort(key=lambda x: x.get(
                    'distance_miles', float('inf')))

            couriers = filtered_couriers

        return couriers, None


def update_courier_availability(courier_id, is_available, current_user):
    """Updates courier availability status - Now allows managers"""
    # Get existing profile
    existing_courier = get_courier_by_id(courier_id)
    if not existing_courier:
        return None, {
            'error': 'Courier not found',
            'code': CourierErrorCodes.COURIER_NOT_FOUND
        }

    # Check access rights - Now allows managers
    if not can_modify_courier(existing_courier, current_user):
        return None, {
            'error': 'Cannot modify other courier profile',
            'code': CourierErrorCodes.CANNOT_MODIFY_OTHER_COURIER
        }

    with get_db() as conn:
        available_since = datetime.utcnow() if is_available else None

        conn.execute("""
            UPDATE courier_profiles
            SET is_available = ?, available_since = ?
            WHERE id = ?
        """, (is_available, available_since, courier_id))

        conn.commit()

    return get_courier_by_id(courier_id), None


def can_modify_courier(courier_data, current_user):
    """Checks if user can modify courier profile - Now allows managers"""
    if not current_user:
        return False

    # Admins and managers can edit any profiles
    if current_user.get('role') in ['admin', 'manager']:
        return True

    # Users can edit only their own profile
    return courier_data.get('user_id') == current_user.get('id')


def can_delete_courier(courier_data, current_user):
    """Checks if user can delete courier profile - Now allows managers"""
    if not current_user:
        return False

    return current_user.get('role') in ['admin', 'manager']
