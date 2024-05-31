import json
import os
import boto3 # type: ignore
from openai import OpenAI # type: ignore

def lambda_handler(event, context):
    question_text = event['question']
    background = event['background']
    
    response_gpt,background_updated = get_response_gpt4(question_text, background)
    response_voice_base64 = invoke_elevenLabs(response_gpt)
    
    # TODO implement
    return {
        'statusCode': 200,
        'body': json.dumps({'response_gpt_voice': response_voice_base64,'background_updated': background_updated})
           #'body': response_gpt
    }
        
def get_response_gpt4(question, background):
    
    key = os.environ.get('api_key')
    
    client = OpenAI(
        api_key = key,
    )
    
    messages = [
        {"role": "system", "content": "Eres un asistente virtual para niños llamado Akira, tus respuestas deben ser cortas y fácil de entender sin importar el tema. No uses numeros en tus respuestas, si te piden alguna operación matemática o fecha, responde en texto, tanto los números de la operación como el resultado damelo en texto. Tus respuestas tienen un tono amable, como una profesora con sus estudiantes pequeños. Saluda cuando sea necesario y menciona los temas en los que eres experta cuando te saluden.Eres experta en historia, geografía, matematica,ciencias y gramática. Eres capaz de personalizar tus respuestas, ya que un niño puede decirte su nombre y debes tenerlo en cuenta. Debes esforzarte en entender la consulta, pero no si es claro, pide que lo vuelvan a preguntar"}
    ]
    
    background_updated = background.copy()
    background_updated.append({"role": "user", "content": question})
    
    messages += background_updated
    
    response = client.chat.completions.create(
        model = "gpt-3.5-turbo",
        messages = messages
    )
    
    response_data = response.choices[0].message.content
    
    background_updated.append({"role": "assistant", "content": response_data})
    
    return response_data, background_updated
    
def invoke_elevenLabs(response):
    lambda_client = boto3.client('lambda')
    
    invoke_event = {
        'response': response
    }
    lambda_elevenLabs_code = os.environ.get('lambda_elevenLabs')
    response_lambda_elevenLabs = lambda_client.invoke(
        FunctionName = lambda_elevenLabs_code,
        InvocationType = 'RequestResponse',
        Payload=json.dumps(invoke_event)
    )
        
    response_elevenLabs = json.loads(response_lambda_elevenLabs['Payload'].read().decode())
        
    response_elevenLabs_json = json.loads(response_elevenLabs['body'])
    #response_text_gpt = response_gpt['body']
    response_voice_base64 = response_elevenLabs_json.get('response_elevenLabs')
    
    return response_voice_base64