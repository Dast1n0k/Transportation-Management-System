from datetime import datetime
from flask import Blueprint, request, jsonify, g
from app.modules.auth.decorators.auth import token_required, admin_required, manager_or_admin_required
from app.modules.hris.services.courier_service import (
    create_courier_profile,
    get_all_couriers,
    get_courier_by_id,
    update_courier_profile,
    delete_courier_profile,
    search_couriers,
    update_courier_availability,
    get_courier_statistics,
    get_courier_by_user_id
)
from app.modules.hris.utils.formatters import (
    format_courier_response,
    format_courier_list_response,
    format_search_response
)
from app.modules.hris.constants import CourierErrorCodes

couriers_routes = Blueprint("couriers_routes", __name__)


@couriers_routes.route("/couriers", methods=["POST"])
@token_required
def create_courier():
    """Create courier profile"""
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON data required'}), 400

    current_user = g.current_user

    courier, error = create_courier_profile(current_user['id'], data)

    if error:
        status_code = 409 if error.get('code') in [
            CourierErrorCodes.PHONE_ALREADY_EXISTS,
            CourierErrorCodes.USER_ALREADY_HAS_COURIER_PROFILE
        ] else 400
        return jsonify(error), status_code

    return jsonify({
        'message': 'Courier profile created successfully',
        'courier': format_courier_response(courier)
    }), 201


@couriers_routes.route("/couriers", methods=["GET"])
@token_required
@manager_or_admin_required
def get_couriers():
    """Get list of all couriers"""
    # Pagination parameters
    limit = request.args.get('limit', 50, type=int)
    offset = request.args.get('offset', 0, type=int)

    # Limit constraint
    limit = min(limit, 100)

    couriers, total = get_all_couriers(limit=limit, offset=offset)

    response = format_courier_list_response(couriers, total)
    response['pagination'] = {
        'limit': limit,
        'offset': offset,
        'has_more': offset + len(couriers) < total
    }

    return jsonify(response), 200


@couriers_routes.route("/couriers/<int:courier_id>", methods=["GET"])
@token_required
def get_courier(courier_id):
    """Get specific courier by ID"""
    current_user = g.current_user
    courier = get_courier_by_id(courier_id)

    if not courier:
        return jsonify({
            'error': 'Courier not found',
            'code': CourierErrorCodes.COURIER_NOT_FOUND
        }), 404

    # Check access rights
    # Admins and managers can see all, regular users can only see their own profile
    if (current_user['role'] not in ['admin', 'manager'] and
            courier['user_id'] != current_user['id']):
        return jsonify({
            'error': 'Access denied',
            'code': CourierErrorCodes.ACCESS_DENIED
        }), 403

    return jsonify({
        'courier': format_courier_response(courier)
    }), 200


@couriers_routes.route("/couriers/my-profile", methods=["GET"])
@token_required
def get_my_courier_profile():
    """Get own courier profile"""
    current_user = g.current_user
    courier = get_courier_by_user_id(current_user['id'])

    if not courier:
        return jsonify({
            'error': 'Courier profile not found',
            'code': CourierErrorCodes.COURIER_NOT_FOUND
        }), 404

    return jsonify({
        'courier': format_courier_response(courier)
    }), 200


@couriers_routes.route("/couriers/<int:courier_id>", methods=["PUT"])
@token_required
@manager_or_admin_required  # Changed: Now requires manager or admin role
def update_courier(courier_id):
    """Update courier profile - Managers and Admins can update any courier"""
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON data required'}), 400

    current_user = g.current_user
    courier, error = update_courier_profile(courier_id, data, current_user)

    if error:
        status_code = {
            CourierErrorCodes.COURIER_NOT_FOUND: 404,
            CourierErrorCodes.PHONE_ALREADY_EXISTS: 409,
            CourierErrorCodes.CANNOT_MODIFY_OTHER_COURIER: 403
        }.get(error.get('code'), 400)
        return jsonify(error), status_code

    return jsonify({
        'message': 'Courier profile updated successfully',
        'courier': format_courier_response(courier)
    }), 200


@couriers_routes.route("/couriers/<int:courier_id>", methods=["DELETE"])
@token_required
@manager_or_admin_required
def delete_courier(courier_id):
    """Delete courier profile - Managers and Admins can delete couriers"""
    current_user = g.current_user
    deleted_courier, error = delete_courier_profile(courier_id, current_user)

    if error:
        status_code = 404 if error.get(
            'code') == CourierErrorCodes.COURIER_NOT_FOUND else 403
        return jsonify(error), status_code

    return jsonify({
        'message': f'Courier profile for {deleted_courier.get("name", "user")} deleted successfully',
        'deleted_by': current_user['username']
    }), 200


@couriers_routes.route("/couriers/search", methods=["GET"])
@token_required
def search_couriers_endpoint():
    """Search couriers by various criteria"""
    # Collect search parameters
    search_params = {}

    # Main filters
    if request.args.get('vehicle_type'):
        search_params['vehicle_type'] = request.args.get('vehicle_type')

    if request.args.get('is_available'):
        search_params['is_available'] = request.args.get(
            'is_available').lower() == 'true'

    if request.args.get('zipcode'):
        search_params['zipcode'] = request.args.get('zipcode')

    if request.args.get('name'):
        search_params['name'] = request.args.get('name')

    if request.args.get('phone'):
        search_params['phone'] = request.args.get('phone')

    # Geo search (now in miles)
    if request.args.get('center_lat') and request.args.get('center_lng'):
        try:
            search_params['center_lat'] = float(request.args.get('center_lat'))
            search_params['center_lng'] = float(request.args.get('center_lng'))
            if request.args.get('radius'):
                search_params['radius'] = float(
                    request.args.get('radius'))  # Now in miles
        except ValueError:
            return jsonify({
                'error': 'Invalid coordinates or radius',
                'code': CourierErrorCodes.VALIDATION_ERROR
            }), 400

    # Sorting
    search_params['sort_by'] = request.args.get('sort_by', 'created_at')
    search_params['sort_order'] = request.args.get('sort_order', 'DESC')

    # Pagination
    search_params['limit'] = min(int(request.args.get('limit', 50)), 100)
    search_params['offset'] = int(request.args.get('offset', 0))

    couriers, error = search_couriers(search_params)

    if error:
        return jsonify(error), 400

    response = format_search_response(couriers, search_params)
    return jsonify(response), 200


@couriers_routes.route("/couriers/<int:courier_id>/availability", methods=["PUT"])
@token_required
@manager_or_admin_required  # Changed: Now requires manager or admin role
def update_availability(courier_id):
    """Update courier availability status - Managers and Admins can update any courier"""
    data = request.get_json()
    if not data:
        return jsonify({'error': 'JSON data required'}), 400

    is_available = data.get('is_available')
    if is_available is None:
        return jsonify({
            'error': 'is_available field is required',
            'code': CourierErrorCodes.VALIDATION_ERROR
        }), 400

    current_user = g.current_user
    courier, error = update_courier_availability(
        courier_id, is_available, current_user)

    if error:
        status_code = {
            CourierErrorCodes.COURIER_NOT_FOUND: 404,
            CourierErrorCodes.CANNOT_MODIFY_OTHER_COURIER: 403
        }.get(error.get('code'), 400)
        return jsonify(error), status_code

    status_text = "available" if is_available else "unavailable"
    return jsonify({
        'message': f'Courier availability updated to {status_text}',
        'courier': format_courier_response(courier)
    }), 200


@couriers_routes.route("/couriers/statistics", methods=["GET"])
@token_required
@manager_or_admin_required
def get_statistics():
    """Get courier statistics"""
    stats = get_courier_statistics()
    return jsonify({
        'statistics': stats,
        'timestamp': datetime.utcnow().isoformat()
    }), 200

# Additional endpoints for convenience


@couriers_routes.route("/couriers/available", methods=["GET"])
@token_required
def get_available_couriers():
    """Get only available couriers"""
    search_params = {
        'is_available': True,
        'sort_by': 'available_since',
        'sort_order': 'ASC',
        'limit': int(request.args.get('limit', 50)),
        'offset': int(request.args.get('offset', 0))
    }

    # Add additional filters if present
    if request.args.get('vehicle_type'):
        search_params['vehicle_type'] = request.args.get('vehicle_type')

    if request.args.get('zipcode'):
        search_params['zipcode'] = request.args.get('zipcode')

    couriers, error = search_couriers(search_params)

    if error:
        return jsonify(error), 400

    return jsonify(format_courier_list_response(couriers)), 200


@couriers_routes.route("/couriers/nearby", methods=["GET"])
@token_required
def get_nearby_couriers():
    """Get nearby couriers (now in miles)"""
    lat = request.args.get('lat')
    lng = request.args.get('lng')
    radius = request.args.get('radius', 15, type=float)

    if not lat or not lng:
        return jsonify({
            'error': 'Latitude and longitude are required',
            'code': CourierErrorCodes.VALIDATION_ERROR
        }), 400

    try:
        search_params = {
            'center_lat': float(lat),
            'center_lng': float(lng),
            'radius': radius,
            'sort_by': 'distance',
            'limit': int(request.args.get('limit', 20)),
            'offset': int(request.args.get('offset', 0))
        }

        # Additional filters
        if request.args.get('vehicle_type'):
            search_params['vehicle_type'] = request.args.get('vehicle_type')

        if request.args.get('available_only', 'false').lower() == 'true':
            search_params['is_available'] = True

        couriers, error = search_couriers(search_params)

        if error:
            return jsonify(error), 400

        response = format_search_response(couriers, search_params)
        response['search_center'] = {
            'lat': search_params['center_lat'],
            'lng': search_params['center_lng']
        }
        response['search_radius_miles'] = radius

        return jsonify(response), 200

    except ValueError:
        return jsonify({
            'error': 'Invalid coordinates',
            'code': CourierErrorCodes.VALIDATION_ERROR
        }), 400
