const fs = require('fs')

const FUNCTION = {
    "$schema": "https://hypar.io/Schemas/Function.json",
    "id": "c918ea51-b05b-4e14-a30b-ea53799365d5",
    "name": "Custom Grids",
    "description": "Create custom gridlines for your project using typical values, relative spacings, or absolute offsets.",
    "language": "C#",
    "model_dependencies": [
        {
            "autohide": false,
            "name": "Envelope",
            "optional": true
        }
    ],
    "model_output": "Grids",
    "input_schema": getInputSchema(),
    "element_types": ["https://prod-api.hypar.io/schemas/GridLine", "https://prod-api.hypar.io/schemas/Envelope", "https://prod-api.hypar.io/schemas/Grid2dElement"],
    "outputs": [],
    "overrides": getOverrides(),
    "repository_url": "",
    "source_file_key": null,
    "preview_image": null
}

function getOrientationConstraints() {
    return {
        "enablePosition": true,
        "enableRotation": true,
        "positionZ": 0,
        "rotationZ": 0
    }
}

function getInputSchema() {
    return {
        "type": "object",
        "properties": {
            "Mode": {
                "type": "string",
                "description": "Are the gridlines calculated from a typical target, as grid positions absolute from their origin, or relative to the last gridline?",
                "$hyparOrder": 0,
                "default": "Typical",
                "$hyparDisableRange": true,
                "enum": [
                    "Absolute",
                    "Relative",
                    "Typical"
                ]
            },
            "Grid Areas": {
                "type": "array",
                "description": "List of grids enclosed by the area they apply to.",
                "$hyparOrder": 1,
                "default": [getDefaultGridArea()],
                "items": {
                    "name": "Grid Area",
                    "type": "object",
                    "required": [],
                    "default": getDefaultGridArea(),
                    "properties": {
                        "Name": {
                            "type": "string",
                            "default": "Main"
                        },
                        "Orientation": {
                            "description": "The origin and rotation of your grid",
                            "$ref": "https://prod-api.hypar.io/schemas/Transform",
                            "$hyparDeprecated": true,
                            "$hyparConstraints": getOrientationConstraints(),
                            "default": getDefaultOrientation()
                        },
                        "U": {
                            "type": "object",
                            "properties": {
                                "Name": {
                                    "type": "string",
                                    "$hyparOrder": 99,
                                    "default": "{A}"
                                },
                                ...getUVProps(),
                            }
                        },
                        "V": {
                            "type": "object",
                            "properties": {
                                "Name": {
                                    "type": "string",
                                    "$hyparOrder": 99,
                                    "default": "{1}"
                                },
                                ...getUVProps(),
                            }
                        }
                    }
                }
            },
            "Show Debug Geometry": {
                "type": "boolean",
                "default": false,
                "$hyparDisableRange": true,
                "$hyparOrder": 2
            }
        }
    }
}

function getDefaultGridArea() {
    return {
        "Name": "Main",
        "Orientation": getDefaultOrientation(),
        "U": {
            "Name": "{A}",
            "Grid Lines": getDefaultGridlines(),
            ...getDefaultTypicalGridUVSettings(),
        },
        "V": {
            "Name": "{1}",
            "Grid Lines": getDefaultGridlines(),
            ...getDefaultTypicalGridUVSettings(),
        }
    }
}

function getDefaultOrientation() {
    return {
        "Matrix": {
            "Components": [
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0
            ]
        }
    }
}

function getDefaultTypicalGridUVSettings() {
    return {
        "Offset Start": 0.5,
        "Target Typical Spacing": 7.6,
        "Offset End": 0.5,
    }
}

function getDefaultGridlines() {
    return [{
        "Spacing": 0.5,
        "Quantity": 1,
        "Location": 0.5
    }, {
        "Spacing": 7.6,
        "Quantity": 2,
        "Location": 8.1
    }]
}

function showIfTypical() {
    return {
        "conditions": [
            {
                "property": "Mode",
                "value": "Typical"
            }
        ]
    }
}

function showIfNotTypical() {
    return {
        "type": "any",
        "conditions": [
            {
                "property": "Mode",
                "value": "Relative"
            },
            {
                "property": "Mode",
                "value": "Absolute"
            }
        ]
    }
}

function showIfRelative() {
    return {
        "conditions": [
            {
                "property": "Mode",
                "value": "Relative"
            }
        ]
    }
}

function showIfAbsolute() {
    return {
        "conditions": [
            {
                "property": "Mode",
                "value": "Absolute"
            }
        ]
    }
}

function getUVProps() {
    return {
        "Spacing": {
            "type": "array",
            "description": "Please use Relative mode and Grid Lines > Spacing instead of Spacing.",
            "$hyparStyle": "singleField",
            "items": {
                "type": "number"
            },
            "$hyparDeprecated": true
        },
        "Grid Lines": {
            "type": "array",
            "$hyparShowIf": showIfNotTypical(),
            "items": {
                "type": "object",
                "$hyparStyle": "row",
                "default": {
                    "Location": 0,
                    "Spacing": 0,
                    "Quantity": 1
                },
                "properties": {
                    "Location": {
                        "type": "number",
                        "$hyparOrder": 1,
                        "$hyparUnitType": "length",
                        "$hyparShowIf": showIfAbsolute()
                    },
                    "Spacing": {
                        "type": "number",
                        "$hyparOrder": 2,
                        "$hyparUnitType": "length",
                        "$hyparShowIf": showIfRelative()
                    },
                    "Quantity": {
                        "type": "integer",
                        "default": 1,
                        "$hyparOrder": 3,
                        "$hyparShowIf": showIfRelative()
                    }
                }
            }
        },
        "Target Typical Spacing": {
            "type": "number",
            "$hyparOrder": 1,
            "$hyparUnitType": "length",
            "default": 7.6,
            "minimum": 3,
            "maximum": 15,
            "$hyparShowIf": showIfTypical(),
        },
        "Offset Start": {
            "type": "number",
            "$hyparOrder": 2,
            "$hyparUnitType": "length",
            "default": 0.5,
            "minimum": 0,
            "maximum": 15,
            "$hyparShowIf": showIfTypical(),
        },
        "Offset End": {
            "type": "number",
            "$hyparOrder": 3,
            "$hyparUnitType": "length",
            "default": 0.5,
            "minimum": 0,
            "maximum": 15,
            "$hyparShowIf": showIfTypical(),
        },
    }
}

function getOverrides() {
    return {
        "Grid Origins": {
            "context": "[*discriminator=Elements.Grid2dElement]",
            // ^ what element does this override apply to? what do you pick when you go to make this override?
            "identity": {
                // How do we know that this override is THIS override?
                // What properties can you use to determine the association
                // between the element and the override? Ideally, this should
                // map to properties on your element, so that when you go back to
                // select the relevant element
                // (which might have moved or been regenerated!!)
                // it knows it's associated with this override.
                "Name": {
                    "type": "string"
                },
            },
            "schema": {
                // More JSON Schema - this time describing what properties you want to be editable.
                // You can have a single geometric property (polygon, etc),
                // or one or more non-geometric properties (for now.)
                // This ideally should *also* map to the properties of your element — if it does,
                // then when you select the element, the input fields will be pre-populated with
                // the current values.
                "Transform": {
                    "$ref": "https://prod-api.hypar.io/schemas/Transform",
                    "$hyparConstraints": getOrientationConstraints(),
                },
            }
        }
    }
}

fs.writeFileSync('hypar.json', JSON.stringify(FUNCTION, null, "  "));