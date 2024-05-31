import json
import base64
from openai import OpenAI # type: ignore
import urllib3 # type: ignore
import os
import boto3 # type: ignore

def lambda_handler(event, context):
    try:

        request_body = event['body'] 
        
        body_dict = json.loads(request_body)
        #model = body_dict['model']
        #language = body_dict['language']
        audio_data = body_dict['file']
        background = body_dict['background']
        
        question_text = get_question_text(audio_data)
        
        response_gpt_voice_base64,background_updated = invoke_gpt(question_text,background)
        
        return {
            'statusCode': 200,
            'body': json.dumps({'response_gpt_voice_base64': response_gpt_voice_base64,'background_updated': background_updated})
            # 'body': response_gpt_voice_base64
        }

    except Exception as e:
        # Si ocurre algún error, devolver un mensaje de error y el código de estado 500
        return {
            'statusCode': 500,
            'body': json.dumps('Error interno del servidor: {}'.format(str(e)))
        }
        
def get_question_text(audio_data):
    audio_bytes = base64.b64decode(audio_data)
    
    wav_file = open("/tmp/temp.wav", "wb")
    wav_file.write(audio_bytes)
    wav_file.close()
    
    audio_file = open("/tmp/temp.wav", "rb")
    
    key = os.environ.get('api_key')
    
    client = OpenAI(
        api_key = key,
    )
    
    transcription = client.audio.transcriptions.create(
            model= 'whisper-1', 
            language = 'es',
            file = audio_file
    )
    
    return transcription.text

def invoke_gpt(question,background):
    lambda_client = boto3.client('lambda')
    
    invoke_event = {
        'question': question,
        'background': background
    }
    lambda_gpt_code = os.environ.get('lambda_gpt')
    response_lambda_gpt = lambda_client.invoke(
        FunctionName = lambda_gpt_code,
        InvocationType = 'RequestResponse',
        Payload=json.dumps(invoke_event)
    )
        
    response_gpt = json.loads(response_lambda_gpt['Payload'].read().decode())
        
    response_gpt_json = json.loads(response_gpt['body'])
    #response_text_gpt = response_gpt['body']
    response_gpt_voice = response_gpt_json.get('response_gpt_voice')
    background_updated = response_gpt_json.get('background_updated')
    
    return response_gpt_voice, background_updated
