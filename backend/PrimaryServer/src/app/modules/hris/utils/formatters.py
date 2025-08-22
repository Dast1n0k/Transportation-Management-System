import math
from datetime import datetime
from app.modules.hris.constants import VehicleTypes, AvailabilityStatus


def format_courier_response(courier_data):
    """Formats courier data for API response"""
    if not courier_data:
        return None

    formatted = dict(courier_data)

    # Format boolean values
    if 'is_available' in formatted:
        formatted['is_available'] = bool(formatted['is_available'])
        formatted['availability_status'] = AvailabilityStatus.STATUS_LABELS.get(
            formatted['is_available'], "Unknown"
        )

    # Format dates
    for date_field in ['available_since', 'created_at', 'updated_at']:
        if date_field in formatted and formatted[date_field]:
            formatted[date_field] = format_datetime(formatted[date_field])

    # Add vehicle type description
    if 'vehicle_type' in formatted and formatted['vehicle_type']:
        formatted['vehicle_type_description'] = VehicleTypes.DESCRIPTIONS.get(
            formatted['vehicle_type'], formatted['vehicle_type']
        )

    # Format coordinates
    if 'latitude' in formatted and 'longitude' in formatted:
        formatted['coordinates'] = {
            'lat': float(formatted['latitude']),
            'lng': float(formatted['longitude'])
        }

    # Parse dimensions
    if 'dimensions' in formatted and formatted['dimensions']:
        formatted['parsed_dimensions'] = parse_dimensions(
            formatted['dimensions'])

    # Format distance in miles if present
    if 'distance_miles' in formatted:
        formatted['distance_display'] = format_distance_miles(
            formatted['distance_miles'])

    return formatted


def format_courier_list_response(couriers, total_count=None):
    """Formats courier list for API response"""
    formatted_couriers = [format_courier_response(
        courier) for courier in couriers]

    response = {
        'couriers': formatted_couriers,
        'count': len(formatted_couriers)
    }

    if total_count is not None:
        response['total'] = total_count

    return response


def format_search_response(couriers, search_params=None):
    """Formats courier search results"""
    response = format_courier_list_response(couriers)

    if search_params:
        response['search_params'] = search_params

    # Add statistics by vehicle types
    vehicle_stats = {}
    availability_stats = {'available': 0, 'unavailable': 0}

    for courier in couriers:
        # Vehicle type statistics
        vehicle_type = courier.get('vehicle_type')
        if vehicle_type:
            vehicle_stats[vehicle_type] = vehicle_stats.get(
                vehicle_type, 0) + 1

        # Availability statistics
        is_available = courier.get('is_available', False)
        if is_available:
            availability_stats['available'] += 1
        else:
            availability_stats['unavailable'] += 1

    response['statistics'] = {
        'by_vehicle_type': vehicle_stats,
        'by_availability': availability_stats
    }

    return response


def format_datetime(dt_string):
    """Formats datetime string"""
    if not dt_string:
        return None

    try:
        # Try parsing different formats
        if isinstance(dt_string, str):
            # SQLite format
            try:
                dt = datetime.strptime(dt_string, '%Y-%m-%d %H:%M:%S')
                return dt.isoformat()
            except ValueError:
                pass

            # ISO format
            try:
                dt = datetime.fromisoformat(dt_string.replace('Z', '+00:00'))
                return dt.isoformat()
            except ValueError:
                pass

        return str(dt_string)
    except Exception:
        return str(dt_string)


def parse_dimensions(dimensions_str):
    """Parses dimension string into structured format"""
    if not dimensions_str:
        return None

    try:
        parts = str(dimensions_str).split('*')
        if len(parts) == 2:
            return {
                'type': '2D',
                'width': int(parts[0]),
                'height': int(parts[1]),
                'format': f"{parts[0]}*{parts[1]}"
            }
        elif len(parts) == 3:
            return {
                'type': '3D',
                'length': int(parts[0]),
                'width': int(parts[1]),
                'height': int(parts[2]),
                'format': f"{parts[0]}*{parts[1]}*{parts[2]}"
            }
    except (ValueError, IndexError):
        pass

    return {
        'type': 'unknown',
        'raw': dimensions_str
    }


def calculate_distance_miles(lat1, lon1, lat2, lon2):
    """Calculates distance between two points in miles (haversine formula)"""
    try:
        # Convert degrees to radians
        lat1, lon1, lat2, lon2 = map(math.radians, [
            float(lat1), float(lon1), float(lat2), float(lon2)
        ])

        dlat = lat2 - lat1
        dlon = lon2 - lon1
        a = math.sin(dlat/2)**2 + math.cos(lat1) * \
            math.cos(lat2) * math.sin(dlon/2)**2
        c = 2 * math.asin(math.sqrt(a))

        r = 3959  # miles

        return c * r

    except (ValueError, TypeError):
        return None


def format_distance_miles(distance_miles):
    """Formats distance for display in miles"""
    if distance_miles is None:
        return None

    if distance_miles < 0.1:
        # Show in feet for very short distances
        feet = int(distance_miles * 5280)
        return f"{feet}ft"
    elif distance_miles < 1:
        return f"{distance_miles:.1f}mi"
    elif distance_miles < 10:
        return f"{distance_miles:.1f}mi"
    else:
        return f"{int(distance_miles)}mi"

def format_capacity(capacity):
    """Formats capacity for US market (pounds and tons)"""
    if not capacity:
        return None

    capacity_str = str(capacity).strip().lower()

    # If units already present, convert to US units if needed
    if 'kg' in capacity_str:
        # Convert kg to lbs
        try:
            kg_value = float(capacity_str.replace('kg', '').strip())
            lbs = int(kg_value * 2.20462)  # kg to lbs conversion
            return f"{lbs}lbs"
        except ValueError:
            pass
    elif 't' in capacity_str and 'ton' not in capacity_str:
        # Convert metric tons to US tons
        try:
            metric_tons = float(capacity_str.replace('t', '').strip())
            us_tons = metric_tons * 1.10231  # metric ton to US ton
            return f"{us_tons:.1f}tons"
        except ValueError:
            pass
    elif any(unit in capacity_str for unit in ['lbs', 'lb', 'pounds', 'tons', 'ton']):
        # Already in US units, return as is
        return capacity_str

    # Try to convert number to appropriate US units
    try:
        num = float(capacity_str)

        # If number is very large, probably kg - convert to lbs
        if num >= 1000:
            lbs = int(num * 2.20462)
            return f"{lbs}lbs"
        # If number is small, probably metric tons - convert to US tons
        elif num < 50:
            us_tons = num * 1.10231
            return f"{us_tons:.1f}tons"
        # Medium numbers could be lbs already
        else:
            return f"{int(num)}lbs"

    except ValueError:
        return capacity_str


def calculate_distance(lat1, lon1, lat2, lon2):
    """Legacy function - redirects to miles calculation for backward compatibility"""
    return calculate_distance_miles(lat1, lon1, lat2, lon2)


def format_distance(distance):
    """Legacy function - redirects to miles formatting for backward compatibility"""
    return format_distance_miles(distance)
