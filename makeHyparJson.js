const fs = require('fs')

const json = {
    "$schema": "https://hypar.io/Schemas/Function.json",
    "id": "c918ea51-b05b-4e14-a30b-ea53799365d5",
    "name": "Custom Grids",
    "description": "",
    "language": "C#",
    "model_dependencies": [
      {
        "autohide": false,
        "name": "Envelope",
        "optional": true
      }
    ],
    "model_output": "Grids",
    "input_schema": {
      "type": "object",
      "properties": {
        "Mode": {
          "type": "string",
          "description": "Are grid positions absolute from their origin, or relative to the last gridline?",
          "$hyparOrder": 0,
          "default": "Relative",
          "$hyparDisableRange": true,
          "enum": [
            "Absolute",
            "Relative"
          ]
        },
        "Grid Areas": {
          "type": "array",
          "description": "List of grids enclosed by the area they apply to.",
          "$hyparOrder": 1,
          "default": [{
            "Name": "Main",
            "Orientation": {
              "Matrix": {
                "Components": [
                  1, 0, 0, 0,
                  0, 1, 0, 0,
                  0, 0, 1, 0
                ]
              }
            },
            "U": {
              "Name": "{A}",
              "Grid Lines": [{
                "Spacing": 0.5,
                "Quantity": 1,
                "Location": 0.5
              }, {
                "Spacing": 7.6,
                "Quantity": 2,
                "Location": 8.1
              }]
            },
            "V": {
              "Name": "{1}",
              "Grid Lines": [{
                "Spacing": 0.5,
                "Quantity": 1,
                "Location": 0.5
              }, {
                "Spacing": 7.6,
                "Quantity": 2,
                "Location": 8.1
              }]
            }
          }],
          "items": {
            "name": "Grid Area",
            "type": "object",
            "required": [],
            "properties": {
              "Name": {
                "type": "string",
                "default": "Main"
              },
              "Orientation": {
                "description": "The origin and rotation of your grid",
                "$ref": "https://prod-api.hypar.io/schemas/Transform",
                "$hyparConstraints": {
                  "enablePosition": true,
                  "enableRotation": true,
                  "positionZ": 0,
                  "rotationZ": 0
                },
                "default": {
                  "Matrix": {
                    "Components": [
                      1, 0, 0, 0,
                      0, 1, 0, 0,
                      0, 0, 1, 0
                    ]
                  }
                }
              },
              "U": {
                "type": "object",
                "properties": {
                  "Name": {
                    "type": "string",
                    "default": "{A}"
                  },
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
                          "$hyparShowIf": {
                            "conditions": [
                              {
                                "property": "Mode",
                                "value": "Absolute"
                              }
                            ]
                          }
                        },
                        "Spacing": {
                          "type": "number",
                          "$hyparOrder": 2,
                          "$hyparUnitType": "length",
                          "$hyparShowIf": {
                            "conditions": [
                              {
                                "property": "Mode",
                                "value": "Relative"
                              }
                            ]
                          }
                        },
                        "Quantity": {
                          "type": "integer",
                          "default": 1,
                          "$hyparOrder": 3,
                          "$hyparShowIf": {
                            "conditions": [
                              {
                                "property": "Mode",
                                "value": "Relative"
                              }
                            ]
                          }
                        }
                      }
                    }
                  }
                }
              },
              "V": {
                "type": "object",
                "properties": {
                  "Name": {
                    "type": "string",
                    "default": "{1}"
                  },
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
                    "items": {
                      "type": "object",
                      "default": {
                        "Location": 0,
                        "Spacing": 0,
                        "Quantity": 1
                      },
                      "$hyparStyle": "row",
                      "properties": {
                        "Location": {
                          "type": "number",
                          "$hyparOrder": 1,
                          "$hyparUnitType": "length",
                          "$hyparShowIf": {
                            "conditions": [
                              {
                                "property": "Mode",
                                "value": "Absolute"
                              }
                            ]
                          }
                        },
                        "Spacing": {
                          "type": "number",
                          "$hyparOrder": 2,
                          "$hyparUnitType": "length",
                          "$hyparShowIf": {
                            "conditions": [
                              {
                                "property": "Mode",
                                "value": "Relative"
                              }
                            ]
                          }
                        },
                        "Quantity": {
                          "type": "integer",
                          "default": 1,
                          "$hyparOrder": 3,
                          "$hyparShowIf": {
                            "conditions": [
                              {
                                "property": "Mode",
                                "value": "Relative"
                              }
                            ]
                          }
                        }
                      }
                    }
                  }
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
    },
    "element_types": ["https://prod-api.hypar.io/schemas/GridLine", "https://prod-api.hypar.io/schemas/Envelope", "https://prod-api.hypar.io/schemas/Grid2dElement"],
    "outputs": [],
    "repository_url": "",
    "source_file_key": null,
    "preview_image": null
}

fs.writeFileSync('hypar.json',  JSON.stringify(json, null, "  "));