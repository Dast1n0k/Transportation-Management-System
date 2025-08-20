from app.modules.auth.constants import ErrorCodes, ValidationRules, UserRoles


def validate_user_data(username, password):
    """Валидирует данные пользователя"""
    if not username or not password:
        return {
            'error': 'Username and password are required',
            'code': ErrorCodes.VALIDATION_ERROR
        }

    if len(username) < ValidationRules.MIN_USERNAME_LENGTH:
        return {
            'error': f'Username must be at least {ValidationRules.MIN_USERNAME_LENGTH} characters',
            'code': ErrorCodes.VALIDATION_ERROR
        }

    if len(password) < ValidationRules.MIN_PASSWORD_LENGTH:
        return {
            'error': f'Password must be at least {ValidationRules.MIN_PASSWORD_LENGTH} characters',
            'code': ErrorCodes.VALIDATION_ERROR
        }

    return None


def validate_password(password):
    """Валидирует пароль"""
    if not password:
        return {
            'error': 'Password is required',
            'code': ErrorCodes.VALIDATION_ERROR
        }

    if len(password) < ValidationRules.MIN_PASSWORD_LENGTH:
        return {
            'error': f'Password must be at least {ValidationRules.MIN_PASSWORD_LENGTH} characters',
            'code': ErrorCodes.VALIDATION_ERROR
        }

    return None


def validate_role(role):
    """Валидирует роль пользователя"""
    if role not in UserRoles.ALL_ROLES:
        return {
            'error': f'Role must be one of: {", ".join(UserRoles.ALL_ROLES)}',
            'code': ErrorCodes.VALIDATION_ERROR
        }

    return None


def validate_user_id(user_id):
    """Валидирует ID пользователя"""
    if not user_id:
        return {
            'error': 'User ID is required',
            'code': 'VALIDATION_ERROR'
        }

    try:
        int(user_id)
    except (ValueError, TypeError):
        return {
            'error': 'Invalid user ID format',
            'code': 'VALIDATION_ERROR'
        }

    return None
