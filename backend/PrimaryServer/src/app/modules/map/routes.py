from flask import Blueprint, request, jsonify
from .services import ZipCodeService

zipcode_bp = Blueprint('zipcode', __name__, url_prefix='/api/zipcode')
zip_service = ZipCodeService()


@zipcode_bp.route('/<zipcode>', methods=['GET'])
def get_zipcode(zipcode):
    """Получение информации о ZIP-коде"""
    try:
        result = zip_service.get_zipcode_info(zipcode)
        return jsonify(result.to_dict())
    except Exception as e:
        return jsonify({'error': str(e)}), 500


@zipcode_bp.route('/search', methods=['POST'])
def search_zipcode():
    """Поиск информации о ZIP-коде через POST"""
    data = request.get_json()
    zipcode = data.get('zipcode')

    if not zipcode:
        return jsonify({'error': 'zipcode is required'}), 400

    try:
        result = zip_service.get_zipcode_info(zipcode)
        return jsonify(result.to_dict())
    except Exception as e:
        return jsonify({'error': str(e)}), 500
