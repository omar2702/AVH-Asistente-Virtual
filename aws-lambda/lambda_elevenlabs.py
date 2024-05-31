import json
import base64
import urllib3 # type: ignore
import os
import base64

def lambda_handler(event, context):
    response_gpt_text = event['response']
    # request_body = event['body']
    # body_dict = json.loads(request_body)
    # response_gpt_text = body_dict['response']
    
    audio_base64 = get_voice_elevenLabs(response_gpt_text)

    # TODO implement
    return {
        'statusCode': 200,
        # 'headers': {
        #     "content-type":"audio/mpeg",
        # },
        'body': json.dumps({'response_elevenLabs': audio_base64})
        # 'body':audio_base64
    }

def get_voice_elevenLabs(text):
    base_url = 'https://api.elevenlabs.io/v1/text-to-speech/'
    voice_id = 'L1QajoRwPFiqw35KD4Ch'
    # voice_id = '9F4C8ztpNUmXkdDDbz3J'
    
    url = base_url + voice_id
    http = urllib3.PoolManager()
    
    key = os.environ.get('api_key')
    
    headers = {
        "Accept": "audio/mpeg",
        'xi-api-key': key,
        'Content-Type': 'application/json',
    }
    json_data = {
        'text': text,
        "model_id": "eleven_multilingual_v1",
        "voice_settings": {
            "stability": 1,
            "similarity_boost": 1
        }
    }
    encoded_data = json.dumps(json_data).encode('utf-8')
    
    response = http.request('POST', url, body=encoded_data, headers=headers)
    #status_code = response.status
    #print("header", response.headers)
    audio_bytes = response.data
    #Codificar los bytes en base64
    audio_base64 = base64.b64encode(audio_bytes).decode('utf-8')
    
    
    return audio_base64