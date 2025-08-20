import re
from app.modules.hris.constants import (
    VehicleTypes, CourierErrorCodes, CourierValidationRules
)


def validate_courier_data(data):
    """Валидирует основные данные курьера"""
    errors = []

    # Проверка имени
    name = data.get('name', '').strip() if data.get('name') else ''
    if name and len(name) < CourierValidationRules.MIN_NAME_LENGTH:
        errors.append(
            f'Name must be at least {CourierValidationRules.MIN_NAME_LENGTH} characters')
    elif name and len(name) > CourierValidationRules.MAX_NAME_LENGTH:
        errors.append(
            f'Name must not exceed {CourierValidationRules.MAX_NAME_LENGTH} characters')

    # Проверка фамилии
    surname = data.get('surname', '').strip() if data.get('surname') else ''
    if surname and len(surname) < CourierValidationRules.MIN_NAME_LENGTH:
        errors.append(
            f'Surname must be at least {CourierValidationRules.MIN_NAME_LENGTH} characters')
    elif surname and len(surname) > CourierValidationRules.MAX_NAME_LENGTH:
        errors.append(
            f'Surname must not exceed {CourierValidationRules.MAX_NAME_LENGTH} characters')

    # Проверка телефона
    phone_error = validate_phone(data.get('phone'))
    if phone_error:
        errors.append(phone_error)

    # Проверка типа транспорта
    vehicle_type_error = validate_vehicle_type(data.get('vehicle_type'))
    if vehicle_type_error:
        errors.append(vehicle_type_error)

    # Проверка размеров
    dimensions_error = validate_dimensions(data.get('dimensions'))
    if dimensions_error:
        errors.append(dimensions_error)

    # Проверка координат
    coordinates_error = validate_coordinates(
        data.get('latitude'), data.get('longitude'))
    if coordinates_error:
        errors.append(coordinates_error)

    # Проверка zipcode
    zipcode_error = validate_zipcode(data.get('zipcode'))
    if zipcode_error:
        errors.append(zipcode_error)

    # Проверка заметок
    notes_error = validate_notes(data.get('notes'))
    if notes_error:
        errors.append(notes_error)

    # Проверка локации
    location_error = validate_location(data.get('location'))
    if location_error:
        errors.append(location_error)

    if errors:
        return {
            'error': '; '.join(errors),
            'code': CourierErrorCodes.VALIDATION_ERROR
        }

    return None


def validate_phone(phone):
    """Валидирует номер телефона"""
    if not phone:
        return "Phone number is required"

    phone = str(phone).strip()
    # Убираем все нецифровые символы кроме +
    clean_phone = re.sub(r'[^\d+]', '', phone)

    if len(clean_phone) < CourierValidationRules.MIN_PHONE_LENGTH:
        return f"Phone number must be at least {CourierValidationRules.MIN_PHONE_LENGTH} digits"

    if len(clean_phone) > CourierValidationRules.MAX_PHONE_LENGTH:
        return f"Phone number must not exceed {CourierValidationRules.MAX_PHONE_LENGTH} digits"

    return None


def validate_vehicle_type(vehicle_type):
    """Валидирует тип транспорта"""
    if not vehicle_type:
        return "Vehicle type is required"

    if vehicle_type not in VehicleTypes.ALL_TYPES:
        return f"Vehicle type must be one of: {', '.join(VehicleTypes.ALL_TYPES)}"

    return None


def validate_dimensions(dimensions):
    """Валидирует размеры транспорта"""
    if not dimensions:
        return None  # Размеры необязательны

    dimensions = str(dimensions).strip()

    # Проверяем формат 2D (например: "92*71")
    if re.match(CourierValidationRules.DIMENSIONS_2D_PATTERN, dimensions):
        return None

    # Проверяем формат 3D (например: "152*94*72")
    if re.match(CourierValidationRules.DIMENSIONS_3D_PATTERN, dimensions):
        return None

    return "Dimensions must be in format '152*94*72' (3D) or '92*71' (2D)"


def validate_coordinates(latitude, longitude):
    """Валидирует координаты"""
    if latitude is None or longitude is None:
        return "Latitude and longitude are required"

    try:
        lat = float(latitude)
        lng = float(longitude)
    except (ValueError, TypeError):
        return "Latitude and longitude must be valid numbers"

    if not (CourierValidationRules.MIN_LATITUDE <= lat <= CourierValidationRules.MAX_LATITUDE):
        return f"Latitude must be between {CourierValidationRules.MIN_LATITUDE} and {CourierValidationRules.MAX_LATITUDE}"

    if not (CourierValidationRules.MIN_LONGITUDE <= lng <= CourierValidationRules.MAX_LONGITUDE):
        return f"Longitude must be between {CourierValidationRules.MIN_LONGITUDE} and {CourierValidationRules.MAX_LONGITUDE}"

    return None


def validate_zipcode(zipcode):
    """Валидирует почтовый индекс"""
    if not zipcode:
        return "Zipcode is required"

    zipcode = str(zipcode).strip()
    if len(zipcode) > CourierValidationRules.MAX_ZIPCODE_LENGTH:
        return f"Zipcode must not exceed {CourierValidationRules.MAX_ZIPCODE_LENGTH} characters"

    return None


def validate_notes(notes):
    """Валидирует заметки"""
    if not notes:
        return None  # Заметки необязательны

    if len(notes) > CourierValidationRules.MAX_NOTES_LENGTH:
        return f"Notes must not exceed {CourierValidationRules.MAX_NOTES_LENGTH} characters"

    return None


def validate_location(location):
    """Валидирует описание локации"""
    if not location:
        return None  # Локация необязательна

    if len(location) > CourierValidationRules.MAX_LOCATION_LENGTH:
        return f"Location must not exceed {CourierValidationRules.MAX_LOCATION_LENGTH} characters"

    return None


def validate_search_params(params):
    """Валидирует параметры поиска"""
    errors = []

    # Валидация радиуса поиска
    radius = params.get('radius')
    if radius is not None:
        try:
            radius = float(radius)
            if radius <= 0 or radius > 600:
                errors.append("Search radius must be between 0 and 500 km")
        except (ValueError, TypeError):
            errors.append("Search radius must be a valid number")

    # Валидация лимита
    limit = params.get('limit')
    if limit is not None:
        try:
            limit = int(limit)
            if limit <= 0 or limit > 100:  # Максимум 100 записей
                errors.append("Limit must be between 1 and 100")
        except (ValueError, TypeError):
            errors.append("Limit must be a valid number")

    # Валидация типа транспорта для поиска
    vehicle_type = params.get('vehicle_type')
    if vehicle_type and vehicle_type not in VehicleTypes.ALL_TYPES:
        errors.append(
            f"Vehicle type must be one of: {', '.join(VehicleTypes.ALL_TYPES)}")

    if errors:
        return {
            'error': '; '.join(errors),
            'code': CourierErrorCodes.VALIDATION_ERROR
        }

    return None
